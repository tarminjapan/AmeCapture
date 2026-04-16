use std::f64::consts::PI;
use std::path::Path;

use ab_glyph::{FontRef, PxScale};
use image::{Rgba, RgbaImage};
use imageproc::drawing::draw_text_mut;
use serde::Deserialize;

use crate::error::{AppError, AppResult};

#[derive(Deserialize)]
struct EditData {
    annotations: Vec<Annotation>,
}

#[derive(Deserialize)]
#[serde(tag = "type")]
enum Annotation {
    #[serde(rename = "arrow")]
    Arrow(ArrowAnnotation),
    #[serde(rename = "text")]
    Text(TextAnnotation),
    #[serde(rename = "rectangle")]
    Rectangle(RectangleAnnotation),
    #[serde(rename = "mosaic")]
    Mosaic(MosaicAnnotation),
    #[serde(rename = "crop")]
    Crop(CropAnnotation),
}

#[derive(Deserialize, Clone)]
#[serde(rename_all = "camelCase")]
struct ArrowAnnotation {
    start_x: f64,
    start_y: f64,
    end_x: f64,
    end_y: f64,
    stroke_color: String,
    stroke_width: u32,
}

#[derive(Deserialize, Clone)]
#[serde(rename_all = "camelCase")]
struct TextAnnotation {
    x: f64,
    y: f64,
    text: String,
    font_size: f64,
    stroke_color: String,
}

#[derive(Deserialize, Clone)]
#[serde(rename_all = "camelCase")]
struct RectangleAnnotation {
    x: f64,
    y: f64,
    width: f64,
    height: f64,
    stroke_color: String,
    stroke_width: u32,
}

#[derive(Deserialize, Clone)]
#[serde(rename_all = "camelCase")]
struct MosaicAnnotation {
    x: f64,
    y: f64,
    width: f64,
    height: f64,
    strength: u32,
}

#[derive(Deserialize, Copy, Clone)]
#[serde(rename_all = "camelCase")]
struct CropAnnotation {
    x: f64,
    y: f64,
    width: f64,
    height: f64,
}

struct Triangle {
    x0: f64,
    y0: f64,
    x1: f64,
    y1: f64,
    x2: f64,
    y2: f64,
}

pub trait EditorService: Send + Sync {
    fn apply_annotations(
        &self,
        source_path: &str,
        output_path: &str,
        annotations_json: &str,
    ) -> AppResult<()>;
}

pub struct DefaultEditorService;

impl DefaultEditorService {
    pub fn new() -> Self {
        Self
    }
}

impl EditorService for DefaultEditorService {
    fn apply_annotations(
        &self,
        source_path: &str,
        output_path: &str,
        annotations_json: &str,
    ) -> AppResult<()> {
        let edit: EditData = serde_json::from_str(annotations_json)
            .map_err(|e| AppError::Editor(format!("Invalid annotation data: {e}")))?;
        let img = image::open(source_path)
            .map_err(|e| AppError::Editor(format!("Failed to open image: {e}")))?;

        let mut rgba = img.to_rgba8();
        let font_data = load_system_font();

        let crop_region = edit.annotations.iter().find_map(|a| match a {
            Annotation::Crop(c) => Some(*c),
            _ => None,
        });

        if let Some(crop) = crop_region {
            rgba = apply_crop(&mut rgba, &crop);
        }

        let offset_x = crop_region.map_or(0.0, |c| c.x.max(0.0).round());
        let offset_y = crop_region.map_or(0.0, |c| c.y.max(0.0).round());

        for annotation in &edit.annotations {
            match annotation {
                Annotation::Arrow(arrow) => {
                    let adjusted = ArrowAnnotation {
                        start_x: arrow.start_x - offset_x,
                        start_y: arrow.start_y - offset_y,
                        end_x: arrow.end_x - offset_x,
                        end_y: arrow.end_y - offset_y,
                        ..arrow.clone()
                    };
                    draw_arrow(&mut rgba, &adjusted);
                }
                Annotation::Text(text) => {
                    let adjusted = TextAnnotation {
                        x: text.x - offset_x,
                        y: text.y - offset_y,
                        ..text.clone()
                    };
                    draw_text_annotation(&mut rgba, &adjusted, font_data.as_deref());
                }
                Annotation::Rectangle(rect) => {
                    let adjusted = RectangleAnnotation {
                        x: rect.x - offset_x,
                        y: rect.y - offset_y,
                        ..rect.clone()
                    };
                    draw_rectangle(&mut rgba, &adjusted);
                }
                Annotation::Mosaic(mosaic) => {
                    let adjusted = MosaicAnnotation {
                        x: mosaic.x - offset_x,
                        y: mosaic.y - offset_y,
                        ..mosaic.clone()
                    };
                    draw_mosaic(&mut rgba, &adjusted);
                }
                Annotation::Crop(_) => {}
            }
        }

        let out_path = Path::new(output_path);
        if let Some(parent) = out_path.parent() {
            std::fs::create_dir_all(parent)?;
        }

        let output_img = image::DynamicImage::ImageRgba8(rgba);
        output_img
            .save(out_path)
            .map_err(|e| AppError::Editor(format!("Failed to save image: {e}")))?;
        tracing::info!("Saved edited image to: {}", output_path);
        Ok(())
    }
}

fn parse_color(color: &str) -> Rgba<u8> {
    let hex = color.trim_start_matches('#');
    if hex.len() < 6 {
        return Rgba([255, 0, 0, 255]);
    }
    let r = u8::from_str_radix(&hex[0..2], 16).unwrap_or(255);
    let g = u8::from_str_radix(&hex[2..4], 16).unwrap_or(0);
    let b = u8::from_str_radix(&hex[4..6], 16).unwrap_or(0);
    Rgba([r, g, b, 255])
}

fn apply_crop(rgba: &mut RgbaImage, crop: &CropAnnotation) -> RgbaImage {
    let img_w = rgba.width();
    let img_h = rgba.height();

    let x0 = crop.x.max(0.0).round() as u32;
    let y0 = crop.y.max(0.0).round() as u32;
    let x1 = (crop.x + crop.width).max(0.0).min(f64::from(img_w)).round() as u32;
    let y1 = (crop.y + crop.height)
        .max(0.0)
        .min(f64::from(img_h))
        .round() as u32;

    let crop_w = x1.saturating_sub(x0);
    let crop_h = y1.saturating_sub(y0);

    if crop_w == 0 || crop_h == 0 {
        return std::mem::take(rgba);
    }

    let dyn_img = image::DynamicImage::ImageRgba8(std::mem::take(rgba));
    let cropped = dyn_img.crop_imm(x0, y0, crop_w, crop_h);
    cropped.to_rgba8()
}

fn load_system_font() -> Option<Vec<u8>> {
    let font_paths: &[&str] = if cfg!(target_os = "windows") {
        &[
            "C:\\Windows\\Fonts\\arial.ttf",
            "C:\\Windows\\Fonts\\segoeui.ttf",
            "C:\\Windows\\Fonts\\meiryo.ttc",
        ]
    } else if cfg!(target_os = "macos") {
        &[
            "/System/Library/Fonts/Helvetica.ttc",
            "/System/Library/Fonts/SFNSMono.ttf",
            "/Library/Fonts/Arial.ttf",
        ]
    } else {
        &[
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
            "/usr/share/fonts/TTF/DejaVuSans.ttf",
        ]
    };

    font_paths.iter().find_map(|p| std::fs::read(p).ok())
}

fn draw_text_annotation(rgba: &mut RgbaImage, text_ann: &TextAnnotation, font_data: Option<&[u8]>) {
    let font_data = match font_data {
        Some(d) => d,
        None => {
            tracing::error!("No suitable font found on system");
            return;
        }
    };

    let font = match FontRef::try_from_slice(font_data) {
        Ok(f) => f,
        Err(e) => {
            tracing::error!("Failed to load font: {}", e);
            return;
        }
    };

    let scale = PxScale {
        x: text_ann.font_size as f32,
        y: text_ann.font_size as f32,
    };
    let color = parse_color(&text_ann.stroke_color);

    let mut y_offset = 0i32;

    for line in text_ann.text.split('\n') {
        draw_text_mut(
            rgba,
            color,
            text_ann.x as i32,
            (text_ann.y - text_ann.font_size) as i32 + y_offset,
            scale,
            &font,
            line,
        );
        y_offset += text_ann.font_size as i32;
    }
}

fn draw_arrow(rgba: &mut RgbaImage, arrow: &ArrowAnnotation) {
    let color = parse_color(&arrow.stroke_color);
    let width = arrow.stroke_width.max(1);

    draw_thick_line(
        rgba,
        arrow.start_x,
        arrow.start_y,
        arrow.end_x,
        arrow.end_y,
        color,
        width,
    );

    let angle = (arrow.end_y - arrow.start_y).atan2(arrow.end_x - arrow.start_x);
    let head_length = f64::from(width) * 4.0;
    draw_arrowhead(rgba, arrow.end_x, arrow.end_y, angle, head_length, color);
}

fn draw_rectangle(rgba: &mut RgbaImage, rect: &RectangleAnnotation) {
    let color = parse_color(&rect.stroke_color);
    let width = rect.stroke_width.max(1);
    let x0 = rect.x;
    let y0 = rect.y;
    let x1 = rect.x + rect.width;
    let y1 = rect.y + rect.height;

    draw_thick_line(rgba, x0, y0, x1, y0, color, width);
    draw_thick_line(rgba, x1, y0, x1, y1, color, width);
    draw_thick_line(rgba, x1, y1, x0, y1, color, width);
    draw_thick_line(rgba, x0, y1, x0, y0, color, width);
}

fn draw_thick_line(
    img: &mut RgbaImage,
    x0: f64,
    y0: f64,
    x1: f64,
    y1: f64,
    color: Rgba<u8>,
    thickness: u32,
) {
    let dx = x1 - x0;
    let dy = y1 - y0;
    let length = (dx * dx + dy * dy).sqrt();
    if length == 0.0 {
        return;
    }

    let half = f64::from(thickness) / 2.0;
    let steps = (length * 2.0).ceil() as u32;
    let img_w = img.width();
    let img_h = img.height();

    for i in 0..=steps {
        let t = f64::from(i) / f64::from(steps);
        let cx = x0 + dx * t;
        let cy = y0 + dy * t;

        let r = half.ceil() as i32;
        for oy in -r..=r {
            for ox in -r..=r {
                if f64::from(ox * ox + oy * oy) <= half * half {
                    let px = (cx + f64::from(ox)).round() as i32;
                    let py = (cy + f64::from(oy)).round() as i32;
                    if px >= 0 && py >= 0 && (px as u32) < img_w && (py as u32) < img_h {
                        img.put_pixel(px as u32, py as u32, color);
                    }
                }
            }
        }
    }
}

fn draw_arrowhead(
    img: &mut RgbaImage,
    tip_x: f64,
    tip_y: f64,
    angle: f64,
    size: f64,
    color: Rgba<u8>,
) {
    let angle1 = angle + PI * 5.0 / 6.0;
    let angle2 = angle - PI * 5.0 / 6.0;

    let triangle = Triangle {
        x0: tip_x,
        y0: tip_y,
        x1: tip_x + size * angle1.cos(),
        y1: tip_y + size * angle1.sin(),
        x2: tip_x + size * angle2.cos(),
        y2: tip_y + size * angle2.sin(),
    };

    fill_triangle(img, &triangle, color);
}

fn fill_triangle(img: &mut RgbaImage, tri: &Triangle, color: Rgba<u8>) {
    let mut vertices = [(tri.x0, tri.y0), (tri.x1, tri.y1), (tri.x2, tri.y2)];
    vertices.sort_by(|a, b| a.1.partial_cmp(&b.1).unwrap_or(std::cmp::Ordering::Equal));

    let (ax, ay) = vertices[0];
    let (bx, by) = vertices[1];
    let (cx, cy) = vertices[2];

    let min_y = ay.floor() as i32;
    let max_y = cy.ceil() as i32;
    let img_w = img.width();
    let img_h = img.height();
    let range = 0.0..=1.0f64;

    for y in min_y..=max_y {
        let yf = f64::from(y);
        let mut intersections: [f64; 4] = [0.0; 4];
        let mut count = 0usize;

        if (ay - cy).abs() > f64::EPSILON {
            let t = (yf - ay) / (cy - ay);
            if range.contains(&t) {
                intersections[count] = ax + t * (cx - ax);
                count += 1;
            }
        }

        if (ay - by).abs() > f64::EPSILON {
            let t = (yf - ay) / (by - ay);
            if range.contains(&t) {
                intersections[count] = ax + t * (bx - ax);
                count += 1;
            }
        }

        if (by - cy).abs() > f64::EPSILON {
            let t = (yf - by) / (cy - by);
            if range.contains(&t) {
                intersections[count] = bx + t * (cx - bx);
                count += 1;
            }
        }

        if count >= 2 {
            intersections[..count]
                .sort_by(|a, b| a.partial_cmp(b).unwrap_or(std::cmp::Ordering::Equal));
            let x_start = intersections[0];
            let x_end = intersections[count - 1];

            let xs = x_start.floor() as i32;
            let xe = x_end.ceil() as i32;

            for x in xs..=xe {
                if x >= 0 && y >= 0 && (x as u32) < img_w && (y as u32) < img_h {
                    img.put_pixel(x as u32, y as u32, color);
                }
            }
        }
    }
}

fn draw_mosaic(rgba: &mut RgbaImage, mosaic: &MosaicAnnotation) {
    let img_w = rgba.width();
    let img_h = rgba.height();

    let x0 = mosaic.x.max(0.0).round() as u32;
    let y0 = mosaic.y.max(0.0).round() as u32;
    let x1 = (mosaic.x + mosaic.width).min(f64::from(img_w)).round() as u32;
    let y1 = (mosaic.y + mosaic.height).min(f64::from(img_h)).round() as u32;

    if x1 <= x0 || y1 <= y0 {
        return;
    }

    let block_size = mosaic.strength.max(1);

    for by in (y0..y1).step_by(block_size as usize) {
        for bx in (x0..x1).step_by(block_size as usize) {
            let bw = block_size.min(x1 - bx);
            let bh = block_size.min(y1 - by);

            // Calculate average color
            let mut r_sum = 0u64;
            let mut g_sum = 0u64;
            let mut b_sum = 0u64;
            let mut a_sum = 0u64;
            let count = u64::from(bw) * u64::from(bh);

            for py in by..by + bh {
                for px in bx..bx + bw {
                    let pixel = rgba.get_pixel(px, py);
                    r_sum += u64::from(pixel[0]);
                    g_sum += u64::from(pixel[1]);
                    b_sum += u64::from(pixel[2]);
                    a_sum += u64::from(pixel[3]);
                }
            }

            let avg_pixel = Rgba([
                (r_sum / count) as u8,
                (g_sum / count) as u8,
                (b_sum / count) as u8,
                (a_sum / count) as u8,
            ]);

            // Fill block
            for py in by..by + bh {
                for px in bx..bx + bw {
                    rgba.put_pixel(px, py, avg_pixel);
                }
            }
        }
    }
}
