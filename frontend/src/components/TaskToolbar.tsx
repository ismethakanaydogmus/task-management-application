import { PRIORITY_LABELS, TASK_PRIORITIES, type SortDirection, type TaskPriority, type TaskSortField } from '../types/task';

// Kullanıcıya tek bir "Sırala" listesi gösteriyoruz; her seçenek backend'deki (alan, yön) çiftine karşılık geliyor.
export const SORT_OPTIONS = [
  { value: 'newest', label: 'En yeni', sortBy: 'createdAt', sortDirection: 'desc' },
  { value: 'oldest', label: 'En eski', sortBy: 'createdAt', sortDirection: 'asc' },
  { value: 'dueSoon', label: 'Son tarihe göre', sortBy: 'dueDate', sortDirection: 'asc' },
  { value: 'priority', label: 'Önceliğe göre', sortBy: 'priority', sortDirection: 'desc' },
  { value: 'title', label: 'Başlık (A–Z)', sortBy: 'title', sortDirection: 'asc' },
] as const satisfies readonly { value: string; label: string; sortBy: TaskSortField; sortDirection: SortDirection }[];

export type SortOption = (typeof SORT_OPTIONS)[number]['value'];

type TaskToolbarProps = {
  search: string;
  onSearchChange: (value: string) => void;
  priority: TaskPriority | null;
  onPriorityChange: (value: TaskPriority | null) => void;
  sort: SortOption;
  onSortChange: (value: SortOption) => void;
};

export function TaskToolbar({ search, onSearchChange, priority, onPriorityChange, sort, onSortChange }: TaskToolbarProps) {
  return (
    <div className="toolbar">
      <div className="field toolbar-search">
        <label htmlFor="search">Ara</label>
        <input
          id="search"
          type="search"
          value={search}
          onChange={(e) => onSearchChange(e.target.value)}
          placeholder="Başlık veya açıklamada ara…"
          maxLength={100}
          autoComplete="off"
        />
      </div>

      <div className="field">
        <label htmlFor="priority-filter">Öncelik</label>
        <select
          id="priority-filter"
          value={priority ?? ''}
          onChange={(e) => onPriorityChange((e.target.value || null) as TaskPriority | null)}
        >
          <option value="">Tümü</option>
          {TASK_PRIORITIES.map((p) => (
            <option key={p} value={p}>
              {PRIORITY_LABELS[p]}
            </option>
          ))}
        </select>
      </div>

      <div className="field">
        <label htmlFor="sort">Sırala</label>
        <select id="sort" value={sort} onChange={(e) => onSortChange(e.target.value as SortOption)}>
          {SORT_OPTIONS.map((o) => (
            <option key={o.value} value={o.value}>
              {o.label}
            </option>
          ))}
        </select>
      </div>
    </div>
  );
}
