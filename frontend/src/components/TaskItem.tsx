import { PRIORITY_LABELS, type TaskResponse } from '../types/task';
import { formatDateTime, isOverdue } from '../utils/date';

// PROPS: Bu component'in dışarıdan aldığı her şey. Tipini açıkça tanımlıyoruz.
// Component props'u DEĞİŞTİREMEZ (salt okunur). Bir şeyi değiştirmek istediğinde
// kendisi değiştirmez, üst component'e "şunu değiştir" diye HABER VERİR (onToggle, onDelete).
type TaskItemProps = {
  task: TaskResponse;
  onToggle: (id: number) => void;
  onDelete: (id: number) => void;
};

// "Sunumsal" (presentational) component: kendi state'i yok, sadece props'a göre çizer.
// Aynı props → her zaman aynı görüntü. Test etmesi ve yeniden kullanması kolay.
export function TaskItem({ task, onToggle, onDelete }: TaskItemProps) {
  const overdue = !task.isCompleted && isOverdue(task.dueDate);

  return (
    // className: HTML'deki "class" (class JavaScript'te ayrılmış bir kelime olduğu için)
    // Şablon string ile koşullu sınıf: tamamlandıysa "done" sınıfı eklenir
    <li className={`task-item ${task.isCompleted ? 'done' : ''}`}>
      <label className="task-check">
        <input
          type="checkbox"
          checked={task.isCompleted}
          onChange={() => onToggle(task.id)}
          aria-label={`${task.title} görevini ${task.isCompleted ? 'geri aç' : 'tamamla'}`}
        />
      </label>

      <div className="task-body">
        <div className="task-title-row">
          <span className="task-title">{task.title}</span>
          {/* data-priority: CSS'te önceliğe göre renk vermek için */}
          <span className="badge" data-priority={task.priority}>
            {PRIORITY_LABELS[task.priority]}
          </span>
        </div>

        {/* Koşullu render: && solu true ise sağını çizer, false/null ise hiçbir şey çizmez */}
        {task.description && <p className="task-desc">{task.description}</p>}

        <div className="task-meta">
          {task.dueDate && (
            <span className={overdue ? 'overdue' : undefined}>
              Son tarih: {formatDateTime(task.dueDate)}
              {overdue && ' · gecikti'}
            </span>
          )}
          {task.completedAt && <span>Tamamlandı: {formatDateTime(task.completedAt)}</span>}
        </div>
      </div>

      <button
        type="button"
        className="btn-icon"
        onClick={() => onDelete(task.id)}
        aria-label={`${task.title} görevini sil`}
        title="Sil"
      >
        ✕
      </button>
    </li>
  );
}
