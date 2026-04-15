import { useEffect } from 'react';
import { useTagStore } from '@/stores/tagStore';
import { useWorkspaceStore } from '@/stores/workspaceStore';
import { useTags } from '@/hooks/useTags';
import { X } from 'lucide-react';

export function TagFilterBar() {
  const tags = useTagStore((s) => s.tags);
  const selectedTagIds = useWorkspaceStore((s) => s.selectedTagIds);
  const setSelectedTagIds = useWorkspaceStore((s) => s.setSelectedTagIds);
  const { loadTags } = useTags();

  useEffect(() => {
    loadTags();
  }, [loadTags]);

  if (tags.length === 0) return null;

  const toggleTag = (tagId: string) => {
    if (selectedTagIds.includes(tagId)) {
      setSelectedTagIds(selectedTagIds.filter((id) => id !== tagId));
    } else {
      setSelectedTagIds([...selectedTagIds, tagId]);
    }
  };

  const clearTags = () => {
    setSelectedTagIds([]);
  };

  return (
    <div className="flex items-center gap-1.5 px-4 py-1.5 border-b border-border bg-muted/30 overflow-x-auto">
      <span className="text-xs text-muted-foreground whitespace-nowrap">タグ:</span>
      {tags.map((tag) => (
        <button
          key={tag.id}
          className={`px-2 py-0.5 text-xs rounded-full whitespace-nowrap transition-colors ${
            selectedTagIds.includes(tag.id)
              ? 'bg-primary text-primary-foreground'
              : 'bg-muted hover:bg-accent'
          }`}
          onClick={() => toggleTag(tag.id)}
        >
          {tag.name}
        </button>
      ))}
      {selectedTagIds.length > 0 && (
        <button
          className="p-0.5 rounded hover:bg-accent"
          onClick={clearTags}
          title="タグフィルターをクリア"
        >
          <X className="w-3 h-3" />
        </button>
      )}
    </div>
  );
}
