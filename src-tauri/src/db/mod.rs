pub mod connection;
pub mod migrations;

use rusqlite::Connection;
use std::sync::Mutex;

/// Shared database connection state
pub struct DbState(pub Mutex<Connection>);
