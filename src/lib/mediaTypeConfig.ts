import type { WorkspaceItem } from '@/types';

export type MediaType = WorkspaceItem['type'];

export interface MediaTypeConfig {
  label: string;
  badgeClass: string;
}

export const MEDIA_TYPE_CONFIG: Record<MediaType, MediaTypeConfig> = {
  image: {
    label: '画像',
    badgeClass: 'bg-blue-500/90 text-white',
  },
  video: {
    label: '動画',
    badgeClass: 'bg-purple-500/90 text-white',
  },
};

export function getTypeLabel(type: MediaType): string {
  return MEDIA_TYPE_CONFIG[type].label;
}
