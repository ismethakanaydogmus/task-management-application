import { useRef, useState } from 'react';
import { FilterBar } from './components/FilterBar';
import { TaskForm } from './components/TaskForm';
import { TaskList } from './components/TaskList';
import type { CreateTaskRequest, TaskFilter, TaskResponse } from './types/task';

// AŞAMA 5: Veri SADECE tarayıcı hafızasında (state). Sayfa yenilenince kaybolur.
// Backend'deki Aşama 1 (InMemoryTaskRepository) ile aynı mantık: önce arayüzü tek başına çalıştır,
// Aşama 6'da bu fonksiyonların içini API çağrılarıyla değiştireceğiz.

const now = new Date().toISOString();
const SAMPLE_TASKS: TaskResponse[] = [
  {
    id: 2,
    title: 'Frontend aşamasını bitir',
    description: 'Component, props ve state kavramlarını öğren',
    isCompleted: false,
    priority: 'High',
    dueDate: null,
    createdAt: now,
    updatedAt: null,
    completedAt: null,
  },
  {
    id: 1,
    title: 'Backend aşamalarını tamamla',
    description: null,
    isCompleted: true,
    priority: 'Medium',
    dueDate: null,
    createdAt: now,
    updatedAt: now,
    completedAt: now,
  },
];

export default function App() {
  // LIFTING STATE UP: Görev listesi App'te duruyor, çünkü onu BİRDEN FAZLA component kullanıyor:
  //  - TaskForm ekliyor, TaskList gösteriyor, TaskItem değiştiriyor, FilterBar sayıları gösteriyor.
  // State, onu kullanan component'lerin EN YAKIN ORTAK ATASINDA tutulur.
  // Veri aşağı (props), olaylar yukarı (onCreate, onToggle, onDelete) akar → "tek yönlü veri akışı".
  const [tasks, setTasks] = useState<TaskResponse[]>(SAMPLE_TASKS);
  const [filter, setFilter] = useState<TaskFilter>('all');

  // useRef: değişince ekranı YENİDEN ÇİZDİRMEYEN hafıza. Sadece bir sonraki id'yi takip etmek için.
  // (Aşama 6'da id'yi backend/veritabanı üretecek, buna gerek kalmayacak.)
  const nextId = useRef(3);

  function handleCreate(request: CreateTaskRequest) {
    const newTask: TaskResponse = {
      id: nextId.current++,
      title: request.title,
      description: request.description ?? null,
      isCompleted: false,
      priority: request.priority ?? 'Medium',
      dueDate: request.dueDate ?? null,
      createdAt: new Date().toISOString(),
      updatedAt: null,
      completedAt: null,
    };

    // IMMUTABILITY: Mevcut diziyi DEĞİŞTİRMİYORUZ (tasks.push YOK), YENİ bir dizi oluşturuyoruz.
    // React "değişti mi?" sorusunu referans karşılaştırmasıyla (eski === yeni) cevaplar.
    // tasks.push(newTask); setTasks(tasks) yazsaydık: dizi aynı referans → React "değişmemiş" der,
    // ekran GÜNCELLENMEZ. Bu, React'ta en sık yapılan hatalardan biridir.
    //
    // Fonksiyonel güncelleme (prev => ...): "en güncel" state üzerinden hesapla.
    // Arka arkaya birden fazla güncelleme olduğunda eski bir değeri ezmemeyi garanti eder.
    setTasks((prev) => [newTask, ...prev]);
  }

  function handleToggle(id: number) {
    const timestamp = new Date().toISOString();
    setTasks((prev) =>
      prev.map((task) =>
        task.id !== id
          ? task // değişmeyen görevler AYNI referansla kalır
          : {
              ...task, // spread: eski nesnenin kopyası + değişen alanlar
              isCompleted: !task.isCompleted,
              updatedAt: timestamp,
              // Backend'deki iş kuralının aynısı (TaskService.UpdateAsync). Aşama 6'da bu kural
              // frontend'den SİLİNECEK; tek doğru kaynak backend olacak, biz onun cevabını göstereceğiz.
              completedAt: task.isCompleted ? null : timestamp,
            },
      ),
    );
  }

  function handleDelete(id: number) {
    setTasks((prev) => prev.filter((task) => task.id !== id));
  }

  // DERIVED STATE (türetilmiş veri): filtrelenmiş liste ve sayılar state'te TUTULMUYOR,
  // her çizimde tasks + filter'dan HESAPLANIYOR.
  // Ayrı bir state'te tutsaydık (const [visibleTasks, setVisibleTasks] = ...):
  // görev eklenince/silinince iki state'i de güncellemeyi unutabilir, ekranda tutarsızlık görürdük.
  // Kural: başka state'ten hesaplanabilen şeyi state'e koyma.
  const visibleTasks = tasks.filter((task) =>
    filter === 'active' ? !task.isCompleted : filter === 'completed' ? task.isCompleted : true,
  );
  const counts: Record<TaskFilter, number> = {
    all: tasks.length,
    active: tasks.filter((t) => !t.isCompleted).length,
    completed: tasks.filter((t) => t.isCompleted).length,
  };

  return (
    <main className="container">
      <header className="app-header">
        <h1>Görev Yöneticisi</h1>
        <p className="subtitle">
          {counts.active === 0 ? 'Tüm görevler tamamlandı 🎉' : `${counts.active} aktif görev`}
        </p>
      </header>

      <section className="card">
        <TaskForm onCreate={handleCreate} />
      </section>

      <section className="card">
        <FilterBar value={filter} onChange={setFilter} counts={counts} />
        <TaskList tasks={visibleTasks} onToggle={handleToggle} onDelete={handleDelete} />
      </section>
    </main>
  );
}
