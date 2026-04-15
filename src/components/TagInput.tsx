import { useState, useRef, useEffect } from 'react';
import type { Tag } from '@/types';
import { X, Plus } from 'lucide-react';
import { useTags } from '@/hooks/useTags';
import { useTagStore } from '@/stores/tagStore';

interface TagInputProps {
  itemId: string;
}

export function TagInput({ itemId }: TagInputProps) {
  const tags = useTagStore((s) => s.tags);
  const itemTags = useTagStore((s) => s.itemTags[itemId] ?? []);
  const [inputValue, setInputValue] = useState('');
  const [showSuggestions, setShowSuggestions] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  const { loadTagsForItem, createTag, addTagToItem, removeTagFromItem, loadTags } = useTags();

  useEffect(() => {
    loadTagsForItem(itemId);
  }, [itemId, loadTagsForItem]);

  const currentTags = itemTags;
  const assignedTagIds = new Set(currentTags.map((t) => t.id));

  const suggestions = tags.filter(
    (t) => !assignedTagIds.has(t.id) && t.name.toLowerCase().includes(inputValue.toLowerCase()),
  );

  const handleAddTag = async (tag: Tag) => {
    await addTagToItem(itemId, tag.id);
    setInputValue('');
    setShowSuggestions(false);
  };

  const handleCreateAndAdd = async () => {
    const name = inputValue.trim();
    if (!name) return;

    const existing = tags.find((t) => t.name.toLowerCase() === name.toLowerCase());
    if (existing) {
      if (!assignedTagIds.has(existing.id)) {
        await addTagToItem(itemId, existing.id);
      }
    } else {
      const newTag = await createTag(name);
      if (newTag) {
        await addTagToItem(itemId, newTag.id);
      }
    }
    setInputValue('');
    setShowSuggestions(false);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      if (inputValue.trim()) {
        if (suggestions.length === 1) {
          handleAddTag(suggestions[0]!);
        } else {
          handleCreateAndAdd();
        }
      }
    } else if (e.key === 'Escape') {
      setShowSuggestions(false);
      setInputValue('');
    }
  };

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap gap-1">
        {currentTags.map((tag) => (
          <span
            key={tag.id}
            className="inline-flex items-center gap-1 px-2 py-0.5 text-xs rounded-full bg-primary/10 text-primary"
          >
            {tag.name}
            <button
              className="hover:text-destructive"
              onClick={() => removeTagFromItem(itemId, tag.id)}
            >
              <X className="w-3 h-3" />
            </button>
          </span>
        ))}
      </div>
      <div className="relative">
        <div className="flex items-center gap-1">
          <input
            ref={inputRef}
            type="text"
            value={inputValue}
            onChange={(e) => {
              setInputValue(e.target.value);
              setShowSuggestions(true);
            }}
            onFocus={() => {
              setShowSuggestions(true);
              if (tags.length === 0) loadTags();
            }}
            onBlur={() => {
              setTimeout(() => setShowSuggestions(false), 200);
            }}
            onKeyDown={handleKeyDown}
            placeholder="タグを追加..."
            className="flex-1 px-2 py-1 text-xs border rounded bg-background focus:outline-none focus:ring-1 focus:ring-ring"
          />
          <button
            className="p-1 rounded hover:bg-accent"
            onClick={handleCreateAndAdd}
            title="タグを作成して追加"
          >
            <Plus className="w-3 h-3" />
          </button>
        </div>
        {showSuggestions && suggestions.length > 0 && (
          <div className="absolute z-10 left-0 right-0 mt-1 bg-background border rounded-md shadow-md max-h-32 overflow-auto">
            {suggestions.map((tag) => (
              <button
                key={tag.id}
                className="w-full text-left px-2 py-1 text-xs hover:bg-accent truncate"
                onMouseDown={(e) => {
                  e.preventDefault();
                  handleAddTag(tag);
                }}
              >
                {tag.name}
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
