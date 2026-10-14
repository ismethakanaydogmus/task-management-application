import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { createTask, deleteTask, getTasks, updateTask } from '../api/tasks';
import type { CreateTaskRequest, TaskResponse, UpdateTaskRequest } from '../types/task';

// SERVER STATE: Görev listesinin ASIL sahibi backend. Tarayıcıdaki sadece önbellekteki bir KOPYA.
// Bu kopyanın ne zaman bayatladığını, ne zaman tekrar çekileceğini TanStack Query yönetir.
//
// Neden useState + useEffect ile elle yazmadık?
//   useEffect(() => { fetch(...).then(setTasks) }, [])
// Bu basit görünür ama şunları SEN çözmek zorunda kalırsın:
//  - loading / error state'leri          - aynı veriyi isteyen 3 component = 3 istek
//  - component kapanınca gelen cevap     - hızlı tıklamada eski cevabın yenisini ezmesi (race condition)
//  - ekleme sonrası listeyi yenilemek     - sekmeye dönünce veriyi tazelemek
//  - StrictMode'da effect'in 2 kez çalışması → 2 istek

// Query key: bu verinin önbellekteki "adresi". Hep aynı diziyi kullanmak için tek yerde tanımlı.
// Aşama 8'de filtre/sayfa eklenince ['tasks', { page: 2, status: 'active' }] gibi genişleyecek.
export const tasksQueryKey = ['tasks'] as const;

export function useTasksQuery() {
  return useQuery({
    queryKey: tasksQueryKey,
    queryFn: ({ signal }) => getTasks(signal),
  });
}

// MUTATION: sunucudaki veriyi DEĞİŞTİREN işlemler (POST/PUT/DELETE).
// Başarılı olunca "görev listesi artık bayat" deyip (invalidate) TanStack Query'nin yeniden çekmesini sağlıyoruz.
//
// Neden kendimiz önbelleğe ekleyip geçmiyoruz da tekrar çekiyoruz?
// Backend'in döndüreceği gerçek hali (id, createdAt, completedAt...) en doğru kaynak backend'dir.
// Aşama 5'te frontend'de kopyaladığımız "CompletedAt" iş kuralı bu sayede SİLİNDİ:
// frontend artık kural uygulamıyor, sadece backend'in söylediğini gösteriyor.

export function useCreateTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateTaskRequest) => createTask(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: tasksQueryKey }),
  });
}

export function useToggleTask() {
  const queryClient = useQueryClient();
  return useMutation({
    // PUT kaynağın TAMAMINI ister: mevcut görevin tüm alanlarını gönderip sadece isCompleted'ı çeviriyoruz.
    // (Bir PATCH /complete endpoint'i olsaydı sadece id göndermek yeterdi.)
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
    onSuccess: () => queryClient.invalidateQueries({ queryKey: tasksQueryKey }),
  });
}

export function useDeleteTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => deleteTask(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: tasksQueryKey }),
  });
}
