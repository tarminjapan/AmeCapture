import { useWorkspaceStore } from "@/stores/workspaceStore";
import { Search } from "lucide-react";

export function SearchBar() {
  const searchQuery = useWorkspaceStore((s) => s.searchQuery);
  const setSearchQuery = useWorkspaceStore((s) => s.setSearchQuery);

  return (
    <div className="relative">
      <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
      <input
        type="text"
        value={searchQuery}
        onChange={(e) => setSearchQuery(e.target.value)}
        placeholder="検索..."
        className="pl-8 pr-3 py-1.5 text-sm border rounded-md bg-background w-48 focus:outline-none focus:ring-2 focus:ring-ring"
      />
    </div>
  );
}