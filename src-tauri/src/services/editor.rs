use std::f64::consts::PI;
use std::path::Path;

use image::{Rgba, RgbaImage};
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
}

#[derive(Deserialize)]
struct ArrowAnnotation {
    start_x: f64,
    start_y: f64,
    end_x: f64,
    end_y: f64,
    stroke_color: String,
    stroke_width: u32,
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
        for annotation in &edit.annotations {
            match annotation {
                Annotation::Arrow(arrow) => draw_arrow(&mut rgba, arrow),
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
