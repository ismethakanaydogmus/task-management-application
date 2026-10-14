import type { TaskFilter } from '../types/task';

type FilterBarProps = {
  value: TaskFilter;
  onChange: (filter: TaskFilter) => void;
  // Veri henüz yüklenmediyse undefined: "0" göstermek "hiç görevin yok" diye yanlış anlaşılır.
  counts?: Record<TaskFilter, number>;
};

const OPTIONS: { value: TaskFilter; label: string }[] = [
  { value: 'all', label: 'Tümü' },
  { value: 'active', label: 'Aktif' },
  { value: 'completed', label: 'Tamamlanan' },
];

export function FilterBar({ value, onChange, counts }: FilterBarProps) {
  return (
    <div className="filter-bar" role="group" aria-label="Görev filtresi">
      {OPTIONS.map((option) => (
        <button
          key={option.value}
          type="button"
          className={option.value === value ? 'active' : undefined}
          aria-pressed={option.value === value}
          onClick={() => onChange(option.value)}
        >
          {option.label} {counts && <span className="count">{counts[option.value]}</span>}
        </button>
      ))}
    </div>
  );
}
