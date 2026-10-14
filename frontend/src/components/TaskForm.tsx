import { useState, type FormEvent } from 'react';
import { ApiError } from '../api/client';
import {
  PRIORITY_LABELS,
  TASK_PRIORITIES,
  type CreateTaskRequest,
  type TaskPriority,
} from '../types/task';
import { localInputToUtcIso } from '../utils/date';

type TaskFormProps = {
  // Artık Promise dönüyor: form, işlemin BAŞARILI olup olmadığını bekleyip ona göre davranacak.
  onCreate: (request: CreateTaskRequest) => Promise<unknown>;
  isSubmitting: boolean;
};

// Alan adı → hata mesajları. Anahtarlar backend'in ProblemDetails "errors" anahtarlarıyla aynı
// ("title", "description", "priority", "dueDate", "body").
type FieldErrors = Record<string, string[]>;

// Backend validator'ı ile AYNI sınırlar (TaskValidators.cs).
const TITLE_MAX = 200;
const DESCRIPTION_MAX = 2000;

export function TaskForm({ onCreate, isSubmitting }: TaskFormProps) {
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [priority, setPriority] = useState<TaskPriority>('Medium');
  const [dueDate, setDueDate] = useState(''); // datetime-local değeri, örn. "2026-10-20T18:00"
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [formError, setFormError] = useState<string | null>(null);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError(null);

    // FRONTEND VALİDASYONU: sadece kullanıcı deneyimi (anında geri bildirim, gereksiz istek yok).
    // Güvenlik için DEĞİL; asıl kontrol backend'de.
    const trimmed = title.trim();
    if (!trimmed) {
      setFieldErrors({ title: ['Başlık boş olamaz.'] });
      return;
    }

    try {
      await onCreate({
        title: trimmed,
        description: description.trim() || null,
        priority,
        dueDate: localInputToUtcIso(dueDate),
      });

      // Formu SADECE başarı durumunda sıfırla. Hata olursa kullanıcının yazdıkları kaybolmasın.
      setTitle('');
      setDescription('');
      setPriority('Medium');
      setDueDate('');
      setFieldErrors({});
    } catch (error) {
      // BACKEND VALİDASYONU: frontend'in bilmediği/kaçırdığı bir kural ihlal edildiyse
      // (veya biri frontend kontrolünü atlattıysa) backend 400 + alan bazlı hatalar döner.
      // Bunları ilgili input'un altında gösteriyoruz.
      if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
        setFieldErrors(error.fieldErrors);
      } else {
        setFormError(error instanceof Error ? error.message : 'Beklenmeyen bir hata oluştu.');
      }
    }
  }

  // Kullanıcı bir alanı düzeltmeye başlayınca o alanın hatasını kaldır.
  function clearFieldError(field: string) {
    if (fieldErrors[field]) {
      setFieldErrors(({ [field]: _removed, ...rest }) => rest);
    }
  }

  return (
    <form className="task-form" onSubmit={handleSubmit} noValidate>
      <div className="field">
        <label htmlFor="title">Başlık</label>
        <input
          id="title"
          value={title}
          onChange={(e) => {
            setTitle(e.target.value);
            clearFieldError('title');
          }}
          maxLength={TITLE_MAX}
          placeholder="Ne yapılacak?"
          aria-invalid={!!fieldErrors.title}
          aria-describedby={fieldErrors.title ? 'title-error' : undefined}
          autoComplete="off"
        />
        <FieldError id="title-error" messages={fieldErrors.title} />
      </div>

      <div className="field">
        <label htmlFor="description">Açıklama (isteğe bağlı)</label>
        <textarea
          id="description"
          value={description}
          onChange={(e) => {
            setDescription(e.target.value);
            clearFieldError('description');
          }}
          maxLength={DESCRIPTION_MAX}
          rows={2}
          aria-invalid={!!fieldErrors.description}
          aria-describedby={fieldErrors.description ? 'description-error' : undefined}
        />
        <FieldError id="description-error" messages={fieldErrors.description} />
      </div>

      <div className="field-row">
        <div className="field">
          <label htmlFor="priority">Öncelik</label>
          <select
            id="priority"
            value={priority}
            onChange={(e) => {
              setPriority(e.target.value as TaskPriority);
              clearFieldError('priority');
            }}
            aria-invalid={!!fieldErrors.priority}
          >
            {TASK_PRIORITIES.map((p) => (
              <option key={p} value={p}>
                {PRIORITY_LABELS[p]}
              </option>
            ))}
          </select>
          <FieldError id="priority-error" messages={fieldErrors.priority} />
        </div>

        <div className="field">
          <label htmlFor="dueDate">Son tarih (isteğe bağlı)</label>
          <input
            id="dueDate"
            type="datetime-local"
            value={dueDate}
            onChange={(e) => {
              setDueDate(e.target.value);
              clearFieldError('dueDate');
            }}
            aria-invalid={!!fieldErrors.dueDate}
            aria-describedby={fieldErrors.dueDate ? 'dueDate-error' : undefined}
          />
          <FieldError id="dueDate-error" messages={fieldErrors.dueDate} />
        </div>
      </div>

      {/* Belirli bir alana ait olmayan hatalar (ağ hatası, 500, "body") */}
      {(formError || fieldErrors.body) && (
        <p className="form-error" role="alert">
          {formError ?? fieldErrors.body?.join(' ')}
        </p>
      )}

      {/* İstek sürerken butonu kilitle: çift tıklama = iki POST = iki görev (POST idempotent değil!) */}
      <button type="submit" className="btn-primary" disabled={isSubmitting}>
        {isSubmitting ? 'Ekleniyor…' : 'Görev ekle'}
      </button>
    </form>
  );
}

function FieldError({ id, messages }: { id: string; messages?: string[] }) {
  if (!messages?.length) return null;
  return (
    <span id={id} className="field-error" role="alert">
      {messages.join(' ')}
    </span>
  );
}
