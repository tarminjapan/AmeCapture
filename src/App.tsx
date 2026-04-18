// export default function App() {
//     return <div>Hello</div>;
// }

import { useState, useCallback } from 'react';
import WorkspacePage from '@/pages/WorkspacePage';
import EditorPage from '@/pages/EditorPage';
import SettingsPage from '@/pages/SettingsPage';

type Page = 'workspace' | 'editor' | 'settings';

export default function App() {
  const [currentPage, setCurrentPage] = useState<Page>('workspace');

  const handleBack = useCallback(() => {
    setCurrentPage('workspace');
  }, []);

  return (
    <div className="h-screen w-screen overflow-hidden" data-tauri-drag-region>
      {currentPage === 'workspace' && (
        <WorkspacePage onNavigateToEditor={() => setCurrentPage('editor')} />
      )}
      {currentPage === 'editor' && <EditorPage onBack={handleBack} />}
      {currentPage === 'settings' && <SettingsPage />}
    </div>
  );
}
