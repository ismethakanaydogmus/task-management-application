import { useState, type FormEvent } from 'react';
import {
  PRIORITY_LABELS,
  TASK_PRIORITIES,
  type CreateTaskRequest,
  type TaskPriority,
} from '../types/task';
import { localInputToUtcIso } from '../utils/date';

type TaskFormProps = {
  onCreate: (request: CreateTaskRequest) => void;
};

// Backend validator'ı ile AYNI sınırlar (TaskValidators.cs).
const TITLE_MAX = 200;
const DESCRIPTION_MAX = 2000;

export function TaskForm({ onCreate }: TaskFormProps) {
  // Formun KENDİ state'i: yazılan ama henüz gönderilmemiş değerler.
  // Bu değerler sadece bu form'u ilgilendirir; App'e taşımaya gerek yok (state'i en yakın yerde tut).
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [priority, setPriority] = useState<TaskPriority>('Medium');
  const [dueDate, setDueDate] = useState(''); // datetime-local değeri, örn. "2026-10-20T18:00"
  const [error, setError] = useState<string | null>(null);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    // Tarayıcının varsayılan davranışı: formu sunucuya gönderip SAYFAYI YENİLEMEK.
    // SPA'da bunu istemiyoruz; her şeyi JavaScript ile biz yöneteceğiz.
    event.preventDefault();

    // FRONTEND VALİDASYONU: sadece kullanıcı deneyimi için (anında geri bildirim, gereksiz istek yok).
    // GÜVENLİK İÇİN DEĞİL! Kötü niyetli biri tarayıcıyı atlayıp API'ye doğrudan istek atabilir.
    // Asıl kontrol her zaman backend'de (Aşama 4).
    const trimmed = title.trim();
    if (!trimmed) {
      setError('Başlık boş olamaz.');
      return;
    }

    onCreate({
      title: trimmed,
      description: description.trim() || null,
      priority,
      dueDate: localInputToUtcIso(dueDate), // yerel saat → UTC ISO ("...Z")
    });

    // Formu sıfırla
    setTitle('');
    setDescription('');
    setPriority('Medium');
    setDueDate('');
    setError(null);
  }

  return (
    <form className="task-form" onSubmit={handleSubmit} noValidate>
      {/*
        CONTROLLED INPUT: input'un değeri React state'inden gelir (value={title}),
        her tuşa basışta state güncellenir (onChange). "Gerçeğin tek kaynağı" state'tir.
        Böylece değeri her an okuyabilir, doğrulayabilir, sıfırlayabiliriz.
        Alternatif (uncontrolled): değeri DOM tutar, gönderirken ref ile okunur. Büyük formlarda
        performans için tercih edilir (react-hook-form bu yaklaşımı kullanır).
      */}
      <div className="field">
        <label htmlFor="title">Başlık</label>
        <input
          id="title"
          value={title}
          onChange={(e) => {
            setTitle(e.target.value);
            if (error) setError(null);
          }}
          maxLength={TITLE_MAX}
          placeholder="Ne yapılacak?"
          aria-invalid={error !== null}
          aria-describedby={error ? 'title-error' : undefined}
          autoComplete="off"
        />
        {error && (
          <span id="title-error" className="field-error" role="alert">
            {error}
          </span>
        )}
      </div>

      <div className="field">
        <label htmlFor="description">Açıklama (isteğe bağlı)</label>
        <textarea
          id="description"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          maxLength={DESCRIPTION_MAX}
          rows={2}
        />
      </div>

      <div className="field-row">
        <div className="field">
          <label htmlFor="priority">Öncelik</label>
          <select
            id="priority"
            value={priority}
            // e.target.value tipi "string"; bizim seçeneklerimizden biri olduğunu biz biliyoruz → "as"
            onChange={(e) => setPriority(e.target.value as TaskPriority)}
          >
            {TASK_PRIORITIES.map((p) => (
              <option key={p} value={p}>
                {PRIORITY_LABELS[p]}
              </option>
            ))}
          </select>
        </div>

        <div className="field">
          <label htmlFor="dueDate">Son tarih (isteğe bağlı)</label>
          <input
            id="dueDate"
            type="datetime-local"
            value={dueDate}
            onChange={(e) => setDueDate(e.target.value)}
          />
        </div>
      </div>

      <button type="submit" className="btn-primary">
        Görev ekle
      </button>
    </form>
  );
}
