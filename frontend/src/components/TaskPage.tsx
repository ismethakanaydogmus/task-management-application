import { useState } from 'react';
import { ApiError } from '../api/client';
import { useAuth } from '../auth/authContext';
import { useDebouncedValue } from '../hooks/useDebouncedValue';
import { useCreateTask, useDeleteTask, useTaskSummary, useTasksQuery, useToggleTask } from '../hooks/useTasks';
import type { TaskFilter, TaskPriority, TaskQuery, TaskResponse } from '../types/task';
import { FilterBar } from './FilterBar';
import { Pagination } from './Pagination';
import { TaskForm } from './TaskForm';
import { TaskList } from './TaskList';
import { SORT_OPTIONS, TaskToolbar, type SortOption } from './TaskToolbar';

const PAGE_SIZE = 10;

// Giriş yapmış kullanıcının ana ekranı.
//  - SERVER STATE (görevler, özet)            → TanStack Query
//  - CLIENT STATE (filtre, arama, sıra, sayfa) → useState
//
// AŞAMA 8: Filtreleme/arama/sıralama/sayfalama artık BACKEND'de (veritabanında, index'lerle).
// Aşama 6'daki "tüm görevleri indir, tarayıcıda süz" yaklaşımı kaldırıldı: 10.000 görevde
// 10.000 kaydı indirip 10'unu göstermek ağ ve bellek israfı.
export function TaskPage() {
  const auth = useAuth();
  const user = auth.status === 'authenticated' ? auth.user : null;

  const [status, setStatus] = useState<TaskFilter>('all');
  const [priority, setPriority] = useState<TaskPriority | null>(null);
  const [search, setSearch] = useState('');
  const [sort, setSort] = useState<SortOption>('newest');
  const [page, setPage] = useState(1);

  // Arama kutusu her tuşta güncellenir (controlled input), ama sorgu 300 ms sessizlikten sonra değişir.
  const debouncedSearch = useDebouncedValue(search, 300);

  const sortOption = SORT_OPTIONS.find((o) => o.value === sort)!;
  const query: TaskQuery = {
    status,
    priority,
    search: debouncedSearch,
    sortBy: sortOption.sortBy,
    sortDirection: sortOption.sortDirection,
    page,
    pageSize: PAGE_SIZE,
  };

  const tasksQuery = useTasksQuery(query);
  const summaryQuery = useTaskSummary();
  const createTask = useCreateTask();
  const toggleTask = useToggleTask();
  const deleteTask = useDeleteTask();

  const result = tasksQuery.data;

  // Sayfadaki SON görev listeden çıkınca (silindi, veya "Aktif" filtresindeyken tamamlandı) o sayfa artık
  // boş kalır: "Sayfa 3 / 2". Kullanıcıyı boş sayfada bırakmamak için bir önceki sayfaya geç.
  //
  // Neden useEffect ile "page > totalPages ise düzelt" demiyoruz? (ilk denediğimiz buydu; lint uyardı)
  // Effect, veri GELDİKTEN SONRA çalışır → önce boş sayfa için istek + render, sonra düzeltme + yeni istek.
  // Sayfanın kaybolmasına KULLANICININ İŞLEMİ yol açtığı için ayarlamayı işlemin başarı anında yapıyoruz.
  function onItemRemovedFromPage() {
    if (result && result.items.length === 1 && page > 1) {
      setPage((p) => p - 1);
    }
  }

  // Filtre/arama/sıralama değişince 1. sayfaya dön. Aksi halde "3. sayfadayken 'Tamamlanan'a geçtim,
  // ama sadece 1 sayfa tamamlanan var" → boş ekran. Bunu effect'te değil OLAY ANINDA yapıyoruz:
  // React önerisi: bir olayın sonucu olan state değişikliği olay işleyicisinde yapılır.
  function changeFilter<T>(setter: (value: T) => void) {
    return (value: T) => {
      setter(value);
      setPage(1);
    };
  }

  const busyTaskId = toggleTask.isPending
    ? (toggleTask.variables?.id ?? null)
    : deleteTask.isPending
      ? (deleteTask.variables ?? null)
      : null;

  const mutationError = toggleTask.error ?? deleteTask.error;
  const summary = summaryQuery.data;
  const hasActiveFilters = status !== 'all' || priority !== null || debouncedSearch.trim() !== '';

  function handleToggle(task: TaskResponse) {
    deleteTask.reset();
    toggleTask.mutate(task, {
      // "Tümü" dışındaki filtrede tamamlama/geri açma görevi bu listeden çıkarır.
      onSuccess: () => status !== 'all' && onItemRemovedFromPage(),
    });
  }

  function handleDelete(id: number) {
    toggleTask.reset();
    deleteTask.mutate(id, { onSuccess: onItemRemovedFromPage });
  }

  return (
    <main className="container">
      <header className="app-header">
        <div className="app-header-row">
          <h1>Görev Yöneticisi</h1>
          {user && (
            <div className="user-menu">
              <span className="user-email" title={user.email}>
                {user.email}
              </span>
              <button type="button" className="btn-secondary" onClick={() => auth.logout()}>
                Çıkış
              </button>
            </div>
          )}
        </div>
        <p className="subtitle">
          {summary && (summary.active === 0 ? 'Aktif görev yok 🎉' : `${summary.active} aktif görev`)}
          {tasksQuery.isFetching && !tasksQuery.isPending && <span className="syncing"> · güncelleniyor…</span>}
        </p>
      </header>

      <section className="card">
        <TaskForm onCreate={createTask.mutateAsync} isSubmitting={createTask.isPending} />
      </section>

      <section className="card">
        <FilterBar value={status} onChange={changeFilter(setStatus)} counts={summary} />
        <TaskToolbar
          search={search}
          onSearchChange={changeFilter(setSearch)}
          priority={priority}
          onPriorityChange={changeFilter(setPriority)}
          sort={sort}
          onSortChange={changeFilter(setSort)}
        />

        {mutationError && (
          <p className="form-error" role="alert">
            {mutationError.message}
          </p>
        )}

        {tasksQuery.isPending ? (
          <p className="empty" role="status">
            Görevler yükleniyor…
          </p>
        ) : tasksQuery.isError && !result ? (
          <ErrorState error={tasksQuery.error} onRetry={() => tasksQuery.refetch()} />
        ) : (
          // isPlaceholderData: Ekrandaki liste ÖNCEKİ sorgunun sonucu (yenisi yolda) → soluk göster.
          <div className={tasksQuery.isPlaceholderData ? 'stale' : undefined} aria-busy={tasksQuery.isPlaceholderData}>
            <TaskList
              tasks={result?.items ?? []}
              onToggle={handleToggle}
              onDelete={handleDelete}
              busyTaskId={busyTaskId}
              emptyMessage={hasActiveFilters ? 'Bu filtrelerle eşleşen görev yok.' : undefined}
            />
            {result && (
              <Pagination
                page={result.page}
                totalPages={result.totalPages}
                totalCount={result.totalCount}
                pageSize={result.pageSize}
                onPageChange={setPage}
                disabled={tasksQuery.isPlaceholderData}
              />
            )}
          </div>
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
      {error instanceof ApiError && error.traceId && <p className="trace">Hata kodu: {error.traceId}</p>}
      <button type="button" className="btn-secondary" onClick={onRetry}>
        Tekrar dene
      </button>
    </div>
  );
}
