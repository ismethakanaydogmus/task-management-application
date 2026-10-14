import { fireEvent, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { ApiError } from '../api/client';
import { TaskForm } from './TaskForm';

// TESTING LIBRARY FELSEFESİ: Component'i KULLANICININ gördüğü ve kullandığı gibi test et.
//  - Elemanları rol ve erişilebilir adla bul: getByRole('button', { name: 'Görev ekle' }), getByLabelText('Başlık')
//  - İç state'e, CSS sınıflarına, component adlarına BAKMA → refactor edince testler kırılmaz.
// Aşama 5'te eklediğimiz <label htmlFor>, role="alert", aria-invalid burada işe yarıyor:
// erişilebilir bir arayüz, test edilmesi de kolay bir arayüzdür.

function setup(onCreate = vi.fn().mockResolvedValue(undefined), isSubmitting = false) {
  const user = userEvent.setup();
  render(<TaskForm onCreate={onCreate} isSubmitting={isSubmitting} />);
  return {
    user,
    onCreate,
    title: () => screen.getByLabelText('Başlık'),
    submit: () => screen.getByRole('button', { name: /görev ekle|ekleniyor/i }),
  };
}

describe('TaskForm', () => {
  it('boş başlıkla gönderilince hata gösterir ve backende istek atmaz', async () => {
    const { user, onCreate, submit, title } = setup();

    await user.type(title(), '   ');
    await user.click(submit());

    expect(screen.getByRole('alert')).toHaveTextContent('Başlık boş olamaz.');
    expect(title()).toHaveAttribute('aria-invalid', 'true');
    expect(onCreate).not.toHaveBeenCalled();
  });

  it('geçerli formu kırpılmış başlık ve UTC tarihle gönderir, sonra formu temizler', async () => {
    const { user, onCreate, submit, title } = setup();

    await user.type(title(), '  Market alışverişi  ');
    await user.selectOptions(screen.getByLabelText('Öncelik'), 'High');
    // datetime-local alanına jsdom'da klavyeyle yazmak desteklenmiyor; değeri doğrudan değiştiriyoruz.
    fireEvent.change(screen.getByLabelText(/son tarih/i), { target: { value: '2026-10-20T18:00' } });
    await user.click(submit());

    expect(onCreate).toHaveBeenCalledWith({
      title: 'Market alışverişi',
      description: null,
      priority: 'High',
      dueDate: '2026-10-20T15:00:00.000Z', // İstanbul 18:00 → UTC 15:00
    });
    expect(title()).toHaveValue(''); // başarı → form sıfırlandı
  });

  it('backend validasyon hatasını ilgili alanın altında gösterir ve yazılanı korur', async () => {
    const backendError = new ApiError(400, 'Bir veya daha fazla alan geçersiz', {
      title: ['Başlık en fazla 200 karakter olabilir.'],
    });
    const { user, submit, title } = setup(vi.fn().mockRejectedValue(backendError));

    await user.type(title(), 'Çok uzun bir başlık');
    await user.click(submit());

    expect(await screen.findByRole('alert')).toHaveTextContent('Başlık en fazla 200 karakter olabilir.');
    expect(title()).toHaveValue('Çok uzun bir başlık'); // kullanıcının yazdığı kaybolmadı
  });

  it('ağ hatasını form seviyesinde gösterir', async () => {
    const { user, submit, title } = setup(vi.fn().mockRejectedValue(new ApiError(0, 'Sunucuya ulaşılamadı.')));

    await user.type(title(), 'Market');
    await user.click(submit());

    expect(await screen.findByRole('alert')).toHaveTextContent('Sunucuya ulaşılamadı.');
  });

  it('gönderim sürerken butonu kilitler (çift tıklama = çift POST olmasın)', () => {
    const { submit } = setup(undefined, true);

    expect(submit()).toBeDisabled();
    expect(submit()).toHaveTextContent('Ekleniyor…');
  });
});
