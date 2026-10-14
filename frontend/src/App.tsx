import { useState } from 'react';
import { ApiError } from './api/client';
import { FilterBar } from './components/FilterBar';
import { TaskForm } from './components/TaskForm';
import { TaskList } from './components/TaskList';
import { useCreateTask, useDeleteTask, useTasksQuery, useToggleTask } from './hooks/useTasks';
import type { TaskFilter, TaskResponse } from './types/task';

// AŞAMA 6: Veri artık BACKEND'de (PostgreSQL). Sayfa yenilenince kaybolmaz.
// Aşama 5'teki useState<TaskResponse[]>, SAMPLE_TASKS, nextId ve frontend'deki "CompletedAt"
// kural kopyası SİLİNDİ. İki tür state'i ayırıyoruz:
//  - SERVER STATE (görevler)        → TanStack Query (useTasksQuery)
//  - CLIENT STATE (seçili filtre)   → useState (sadece bu tarayıcıyı ilgilendiriyor)

export default function App() {
  const [filter, setFilter] = useState<TaskFilter>('all');

  const tasksQuery = useTasksQuery();
  const createTask = useCreateTask();
  const toggleTask = useToggleTask();
  const deleteTask = useDeleteTask();

  // Hangi görev için istek sürüyor? (o satırın checkbox/sil butonunu kilitlemek için)
  // mutation.variables: mutate()'e verilen son argüman.
  const busyTaskId = toggleTask.isPending
    ? (toggleTask.variables?.id ?? null)
    : deleteTask.isPending
      ? (deleteTask.variables ?? null)
      : null;

  // Toggle/delete hatası (ör. görevi başka bir sekmede silmişsin → 404)
  const mutationError = toggleTask.error ?? deleteTask.error;

  const tasks: TaskResponse[] = tasksQuery.data ?? [];

  // DERIVED STATE: state'te tutulmuyor, her çizimde hesaplanıyor.
  const visibleTasks = tasks.filter((task) =>
    filter === 'active' ? !task.isCompleted : filter === 'completed' ? task.isCompleted : true,
  );
  const counts: Record<TaskFilter, number> = {
    all: tasks.length,
    active: tasks.filter((t) => !t.isCompleted).length,
    completed: tasks.filter((t) => t.isCompleted).length,
  };

  function handleToggle(task: TaskResponse) {
    deleteTask.reset();
    toggleTask.mutate(task);
  }

  function handleDelete(id: number) {
    toggleTask.reset();
    deleteTask.mutate(id);
  }

  return (
    <main className="container">
      <header className="app-header">
        <h1>Görev Yöneticisi</h1>
        <p className="subtitle">
          {tasksQuery.isSuccess &&
            (counts.active === 0 ? 'Aktif görev yok 🎉' : `${counts.active} aktif görev`)}
          {/* isFetching: arka planda tazeleme sürüyor (ilk yükleme değil) */}
          {tasksQuery.isFetching && !tasksQuery.isPending && <span className="syncing"> · güncelleniyor…</span>}
        </p>
      </header>

      <section className="card">
        {/* mutateAsync: Promise döner → TaskForm başarıyı/hatayı bekleyebilir */}
        <TaskForm onCreate={createTask.mutateAsync} isSubmitting={createTask.isPending} />
      </section>

      <section className="card">
        <FilterBar value={filter} onChange={setFilter} counts={tasksQuery.isSuccess ? counts : undefined} />

        {mutationError && (
          <p className="form-error" role="alert">
            {mutationError.message}
          </p>
        )}

        {/*
          Sunucudan veri çekerken ekranın 3 olası durumu var. Üçünü de AÇIKÇA ele alıyoruz:
           isPending → ilk yükleme (henüz hiç veri yok)
           isError   → hata (ve önbellekte gösterilecek veri yok)
           başarılı  → liste (boşsa TaskList "empty state" gösterir)
        */}
        {tasksQuery.isPending ? (
          <p className="empty" role="status">
            Görevler yükleniyor…
          </p>
        ) : tasksQuery.isError ? (
          <ErrorState error={tasksQuery.error} onRetry={() => tasksQuery.refetch()} />
        ) : (
          <TaskList
            tasks={visibleTasks}
            onToggle={handleToggle}
            onDelete={handleDelete}
            busyTaskId={busyTaskId}
          />
        )}
      </section>
    </main>
  );
}

function ErrorState({ error, onRetry }: { error: Error; onRetry: () => void }) {
  return (
    <div className="error-state" role="alert">
      <p>
        <strong>Görevler yüklenemedi.</strong> {error.message}
      </p>
      {/* traceId: kullanıcı bunu destek ekibine iletirse backend log'unda tam hatayı buluruz (Aşama 4) */}
      {error instanceof ApiError && error.traceId && (
        <p className="trace">Hata kodu: {error.traceId}</p>
      )}
      <button type="button" className="btn-secondary" onClick={onRetry}>
        Tekrar dene
      </button>
    </div>
  );
}
