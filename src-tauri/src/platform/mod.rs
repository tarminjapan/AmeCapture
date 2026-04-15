use crate::error::AppResult;
use crate::models::capture::WindowInfo;

// TODO: Implement monitor enumeration
// pub fn enumerate_monitors() -> AppResult<Vec<MonitorInfo>> {
//     tracing::debug!("Enumerating monitors (not yet implemented)");
//     Ok(vec![])
// }
//
// #[derive(Debug, Clone)]
// pub struct MonitorInfo {
//     pub id: u32,
//     pub name: String,
//     pub bounds: (i32, i32, i32, i32),
//     pub is_primary: bool,
// }

#[cfg(target_os = "windows")]
pub fn enumerate_windows() -> AppResult<Vec<WindowInfo>> {
    use windows::Win32::UI::WindowsAndMessaging::EnumWindows;

    let windows: Vec<WindowInfo> = Vec::new();
    let boxed = Box::new(windows);
    let ptr = Box::into_raw(boxed);

    let result = unsafe {
        EnumWindows(
            Some(enum_windows_callback),
            windows::Win32::Foundation::LPARAM(ptr as isize),
        )
    };

    let mut found = if result.is_ok() {
        let boxed = unsafe { Box::from_raw(ptr) };
        *boxed
    } else {
        let _ = unsafe { Box::from_raw(ptr) };
        Vec::new()
    };

    found.sort_by(|a, b| a.title.to_lowercase().cmp(&b.title.to_lowercase()));
    Ok(found)
}

#[cfg(target_os = "windows")]
unsafe extern "system" fn enum_windows_callback(
    hwnd: windows::Win32::Foundation::HWND,
    lparam: windows::Win32::Foundation::LPARAM,
) -> windows::Win32::Foundation::BOOL {
    use windows::Win32::Foundation::TRUE;
    use windows::Win32::Graphics::Dwm::{DwmGetWindowAttribute, DWMWA_EXTENDED_FRAME_BOUNDS};
    use windows::Win32::UI::WindowsAndMessaging::{
        GetClassNameW, GetWindowLongPtrW, GetWindowRect, GetWindowTextW, IsIconic, IsWindowVisible,
        GWL_STYLE, WS_DISABLED,
    };

    let windows = &mut *(lparam.0 as *mut Vec<WindowInfo>);

    if !IsWindowVisible(hwnd).as_bool() {
        return TRUE;
    }

    let style = GetWindowLongPtrW(hwnd, GWL_STYLE);
    if (style & WS_DISABLED.0 as isize) != 0 {
        return TRUE;
    }

    if IsIconic(hwnd).as_bool() {
        return TRUE;
    }

    let mut title_buf = [0u16; 512];
    let title_len = GetWindowTextW(hwnd, &mut title_buf);
    if title_len == 0 {
        return TRUE;
    }
    let title = String::from_utf16_lossy(&title_buf[..title_len as usize]);
    if title.trim().is_empty() {
        return TRUE;
    }

    let mut class_buf = [0u16; 256];
    let class_len = GetClassNameW(hwnd, &mut class_buf);
    let class_name = String::from_utf16_lossy(&class_buf[..class_len as usize]);

    let mut rect: windows::Win32::Foundation::RECT = std::mem::zeroed();
    let hr = DwmGetWindowAttribute(
        hwnd,
        DWMWA_EXTENDED_FRAME_BOUNDS,
        &mut rect as *mut _ as *mut _,
        std::mem::size_of::<windows::Win32::Foundation::RECT>() as u32,
    );
    let bounds = if hr.is_ok() {
        (
            rect.left,
            rect.top,
            rect.right - rect.left,
            rect.bottom - rect.top,
        )
    } else {
        let mut rect2 = std::mem::zeroed();
        let _ = GetWindowRect(hwnd, &mut rect2);
        (
            rect2.left,
            rect2.top,
            rect2.right - rect2.left,
            rect2.bottom - rect2.top,
        )
    };

    windows.push(WindowInfo {
        hwnd: hwnd.0 as isize,
        title,
        class_name,
        bounds,
    });

    TRUE
}

#[cfg(not(target_os = "windows"))]
pub fn enumerate_windows() -> AppResult<Vec<WindowInfo>> {
    Ok(vec![])
}
