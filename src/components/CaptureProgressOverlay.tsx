import { Monitor } from 'lucide-react';

export function CaptureProgressOverlay() {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70">
      <div className="flex flex-col items-center gap-4 rounded-lg border border-border bg-background px-10 py-8 shadow-2xl">
        <div className="flex items-center gap-2 text-primary">
          <Monitor className="h-6 w-6" />
          <div className="h-6 w-6 animate-spin rounded-full border-2 border-primary border-t-transparent" />
        </div>
        <p className="text-sm font-medium">キャプチャ中、少々お待ち下さい</p>
      </div>
    </div>
  );
}
