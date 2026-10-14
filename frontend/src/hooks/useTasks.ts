import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { createTask, deleteTask, getTaskSummary, getTasks, updateTask } from '../api/tasks';
import type { CreateTaskRequest, TaskQuery, TaskResponse, UpdateTaskRequest } from '../types/task';

// SERVER STATE: Görevlerin asıl sahibi backend; burada sadece önbellekteki kopyalar var.
//
// QUERY KEY HİYERARŞİSİ:
//   ['tasks']                         ← kök: invalidate edilince ALTINDAKİ HER ŞEY bayatlar
//   ['tasks', 'list', { ...query }]   ← her filtre/sayfa kombinasyonu AYRI önbellek girdisi
//   ['tasks', 'summary']              ← filtre rozetlerindeki sayılar
// "Aktif" filtresinden "Tümü"ne geçip geri dönünce "Aktif" listesi önbellekten ANINDA gelir.
export const taskKeys = {
  all: ['tasks'] as const,
  list: (query: TaskQuery) => ['tasks', 'list', query] as const,
  summary: ['tasks', 'summary'] as const,
};

export function useTasksQuery(query: TaskQuery) {
  return useQuery({
    queryKey: taskKeys.list(query),
    queryFn: ({ signal }) => getTasks(query, signal),
    // Sayfa/filtre değişince YENİ veri gelene kadar ÖNCEKİ listeyi göster (isPlaceholderData = true).
    // Bu olmasaydı her sayfa geçişinde liste bir anlığına kaybolup "Yükleniyor…" yazardı (titreme).
    placeholderData: keepPreviousData,
  });
}

export function useTaskSummary() {
  return useQuery({
    queryKey: taskKeys.summary,
    queryFn: ({ signal }) => getTaskSummary(signal),
  });
}

// Bir görev eklendi/değişti/silindi → hem listeler hem özet bayatladı.
// Kök anahtarı ['tasks'] invalidate etmek ikisini de kapsar; ekranda olan sorgular hemen yeniden çekilir,
// olmayanlar (başka filtrelerin önbelleği) bir dahaki kullanımda çekilir.
function useInvalidateTasks() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: taskKeys.all });
}

export function useCreateTask() {
  const invalidate = useInvalidateTasks();
  return useMutation({
    mutationFn: (request: CreateTaskRequest) => createTask(request),
    onSuccess: invalidate,
  });
}

export function useToggleTask() {
  const invalidate = useInvalidateTasks();
  return useMutation({
    // PUT kaynağın TAMAMINI ister: mevcut görevin tüm alanlarını gönderip sadece isCompleted'ı çeviriyoruz.
    mutationFn: (task: TaskResponse) => {
      const request: UpdateTaskRequest = {
        title: task.title,
        description: task.description,
        isCompleted: !task.isCompleted,
        priority: task.priority,
        dueDate: task.dueDate,
      };
      return updateTask(task.id, request);
    },
    onSuccess: invalidate,
  });
}

export function useDeleteTask() {
  const invalidate = useInvalidateTasks();
  return useMutation({
    mutationFn: (id: number) => deleteTask(id),
    onSuccess: invalidate,
  });
}
