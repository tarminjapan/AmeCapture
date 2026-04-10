import { useState } from 'react';
import WorkspacePage from '@/pages/WorkspacePage';
import EditorPage from '@/pages/EditorPage';
import SettingsPage from '@/pages/SettingsPage';

type Page = 'workspace' | 'editor' | 'settings';

export default function App() {
  const [currentPage, setCurrentPage] = useState<Page>('workspace');

  return (
    <div className="h-screen w-screen overflow-hidden" data-tauri-drag-region>
      {currentPage === 'workspace' && <WorkspacePage />}
      {currentPage === 'editor' && <EditorPage />}
      {currentPage === 'settings' && <SettingsPage />}
    </div>
  );
}
