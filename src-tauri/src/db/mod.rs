pub mod connection;
pub mod migrations;

use std::sync::Mutex;
use rusqlite::Connection;

/// Shared database connection state
pub struct DbState(pub Mutex<Connection>);