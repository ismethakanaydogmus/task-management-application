import type { TaskResponse } from '../types/task';
import { TaskItem } from './TaskItem';

type TaskListProps = {
  tasks: TaskResponse[];
  onToggle: (id: number) => void;
  onDelete: (id: number) => void;
};

export function TaskList({ tasks, onToggle, onDelete }: TaskListProps) {
  // "Empty state": liste boşken kullanıcıya boş bir ekran yerine ne yapacağını söyle.
  if (tasks.length === 0) {
    return <p className="empty">Burada görev yok. Yukarıdan yeni bir görev ekleyebilirsin.</p>;
  }

  return (
    <ul className="task-list">
      {tasks.map((task) => (
        // KEY: React, listedeki her elemanı bir sonraki çizimde TANIYABİLMEK için benzersiz bir anahtar ister.
        // Hangi eleman eklendi, silindi, yer değiştirdi → buna bakarak sadece değişeni günceller.
        //
        // key={index} kullansaydık: ortadan bir görev silinince alttaki görevlerin index'i kayar,
        // React yanlış elemanları "aynı" sanır; checkbox durumu, input içeriği gibi şeyler
        // YANLIŞ satıra geçebilir. Kalıcı ve benzersiz olan veritabanı id'si doğru anahtardır.
        <TaskItem key={task.id} task={task} onToggle={onToggle} onDelete={onDelete} />
      ))}
    </ul>
  );
}
