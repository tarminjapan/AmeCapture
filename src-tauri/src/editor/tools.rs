use crate::utils::error::AppResult;

/// Apply annotation to image
/// TODO: Implement image annotation tools
pub fn apply_arrow(
    _image: &mut image::RgbaImage,
    _start: (i32, i32),
    _end: (i32, i32),
    _color: image::Rgba<u8>,
    _width: u32,
) -> AppResult<()> {
    Ok(())
}

pub fn apply_rectangle(
    _image: &mut image::RgbaImage,
    _bounds: (i32, i32, i32, i32),
    _color: image::Rgba<u8>,
    _width: u32,
) -> AppResult<()> {
    Ok(())
}

pub fn apply_mosaic(
    _image: &mut image::RgbaImage,
    _bounds: (i32, i32, i32, i32),
    _block_size: u32,
) -> AppResult<()> {
    Ok(())
}
