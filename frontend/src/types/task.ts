// Backend DTO'larının (TaskManager.Application/Tasks/*.cs) TypeScript karşılıkları.
// Alan adları JSON'daki gibi camelCase: C#'taki "IsCompleted" → JSON'da "isCompleted".
//
// ÖNEMLİ: Bu tipler sadece DERLEME ZAMANINDA vardır. Tarayıcıya giden JavaScript'te iz bırakmazlar.
// Yani backend gerçekte farklı bir şey gönderirse TypeScript bunu çalışma anında YAKALAMAZ.
// (İleride: OpenAPI'den otomatik üretim veya Zod ile çalışma anı doğrulaması.)

// C# enum'ının karşılığı: "union type" (birleşim tipi).
// Neden TS "enum" değil? tsconfig'deki "erasableSyntaxOnly" enum'ı yasaklıyor:
// enum, JavaScript'e çevrilirken gerçek bir nesne ÜRETİR (tipler gibi silinmez).
// Union type ise tamamen silinir, sıfır maliyetli ve backend'in gönderdiği metinle ("High") birebir aynı.
export type TaskPriority = 'Low' | 'Medium' | 'High';

// Döngüyle select'e seçenek basmak için değerlerin listesi (tip tek başına çalışma anında yok).
export const TASK_PRIORITIES: readonly TaskPriority[] = ['Low', 'Medium', 'High'];

export const PRIORITY_LABELS: Record<TaskPriority, string> = {
  Low: 'Düşük',
  Medium: 'Orta',
  High: 'Yüksek',
};

// ↔ C#: record TaskResponse(...)
export type TaskResponse = {
  id: number;
  title: string;
  description: string | null;   // C#: string?  → TS: string | null
  isCompleted: boolean;
  priority: TaskPriority;
  // Tarihler JSON'da METİN olarak gelir ("2026-10-20T15:00:00Z"), Date nesnesi olarak değil.
  // Ekranda gösterirken new Date(...) ile çeviriyoruz.
  dueDate: string | null;
  createdAt: string;
  updatedAt: string | null;
  completedAt: string | null;
};

// ↔ C#: record CreateTaskRequest(...)
export type CreateTaskRequest = {
  title: string;
  description?: string | null;  // "?" = gönderilmeyebilir (C#'ta varsayılan değeri olan parametre)
  priority?: TaskPriority;
  dueDate?: string | null;      // UTC ISO formatında: "2026-10-20T15:00:00.000Z"
};

// ↔ C#: record UpdateTaskRequest(...)  — PUT olduğu için tüm alanlar zorunlu
export type UpdateTaskRequest = {
  title: string;
  description: string | null;
  isCompleted: boolean;
  priority: TaskPriority;
  dueDate: string | null;
};

// ↔ C#: enum TaskStatusFilter (backend enum'ları query string'de büyük/küçük harf duyarsız okur)
export type TaskFilter = 'all' | 'active' | 'completed';

// ↔ C#: enum TaskSortField / SortDirection
export type TaskSortField = 'createdAt' | 'dueDate' | 'priority' | 'title';
export type SortDirection = 'asc' | 'desc';

// ↔ C#: record TaskQuery — GET /api/tasks?status=...&page=...
export type TaskQuery = {
  status: TaskFilter;
  priority: TaskPriority | null;
  search: string;
  sortBy: TaskSortField;
  sortDirection: SortDirection;
  page: number;
  pageSize: number;
};

// ↔ C#: record PagedResult<T>
export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
};

// ↔ C#: record TaskSummary — GET /api/tasks/summary
export type TaskSummary = Record<TaskFilter, number>;
