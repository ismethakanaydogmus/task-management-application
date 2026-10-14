import type { TaskResponse } from '../types/task';
import { TaskItem } from './TaskItem';

type TaskListProps = {
  tasks: TaskResponse[];
  onToggle: (task: TaskResponse) => void;
  onDelete: (id: number) => void;
  busyTaskId: number | null; // hangi görev için istek sürüyor
  emptyMessage?: string; // filtre varken "eşleşen yok", yokken "henüz görev yok"
};

export function TaskList({ tasks, onToggle, onDelete, busyTaskId, emptyMessage }: TaskListProps) {
  // "Empty state": liste boşken kullanıcıya boş bir ekran yerine ne yapacağını söyle.
  if (tasks.length === 0) {
    return <p className="empty">{emptyMessage ?? 'Burada görev yok. Yukarıdan yeni bir görev ekleyebilirsin.'}</p>;
  }

  return (
    <ul className="task-list">
      {tasks.map((task) => (
        // KEY: React, listedeki her elemanı bir sonraki çizimde TANIYABİLMEK için benzersiz bir anahtar ister.
        // key={index} kullansaydık: ortadan bir görev silinince alttaki görevlerin index'i kayar,
        // React yanlış elemanları "aynı" sanır. Kalıcı ve benzersiz olan veritabanı id'si doğru anahtardır.
        <TaskItem
          key={task.id}
          task={task}
          onToggle={onToggle}
          onDelete={onDelete}
          busy={task.id === busyTaskId}
        />
      ))}
    </ul>
  );
}
