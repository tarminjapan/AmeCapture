import fs from 'fs';
import path from 'path';

function walkDir(dir, callback) {
  fs.readdirSync(dir).forEach((f) => {
    let dirPath = path.join(dir, f);
    let isDirectory = fs.statSync(dirPath).isDirectory();
    if (isDirectory) {
      if (f !== 'target') {
        walkDir(dirPath, callback);
      }
    } else if (f.endsWith('.rs')) {
      callback(path.join(dir, f));
    }
  });
}

const targetDir = path.resolve(process.cwd(), 'src-tauri/src');
let failed = false;

walkDir(targetDir, (filePath) => {
  const content = fs.readFileSync(filePath, 'utf-8');
  if (/(?:#|#!)\s*\[\s*allow\s*\(/.test(content)) {
    console.error(
      `\x1b[31m[ERROR]\x1b[0m Found 'allow(' in ${filePath}. Using #[allow(...)] or #![allow(...)] is strictly forbidden.`,
    );
    failed = true;
  }
});

if (failed) {
  process.exit(1);
} else {
  console.log('\x1b[32m[SUCCESS]\x1b[0m No allow attributes found in Rust code.');
}
