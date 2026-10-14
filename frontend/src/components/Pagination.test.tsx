import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { Pagination } from './Pagination';

describe('Pagination', () => {
  it('aralığı ve sayfa bilgisini gösterir', () => {
    render(<Pagination page={2} totalPages={3} totalCount={25} pageSize={10} onPageChange={vi.fn()} />);

    expect(screen.getByText('11–20 / 25 görev')).toBeInTheDocument();
    expect(screen.getByText('Sayfa 2 / 3')).toBeInTheDocument();
  });

  it('son sayfada aralığın sonunu toplam sayıyla sınırlar', () => {
    render(<Pagination page={3} totalPages={3} totalCount={25} pageSize={10} onPageChange={vi.fn()} />);

    expect(screen.getByText('21–25 / 25 görev')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /sonraki/i })).toBeDisabled();
  });

  it('ilk sayfada Önceki pasif; Sonraki bir sonraki sayfayı ister', async () => {
    const onPageChange = vi.fn();
    render(<Pagination page={1} totalPages={3} totalCount={25} pageSize={10} onPageChange={onPageChange} />);

    expect(screen.getByRole('button', { name: /önceki/i })).toBeDisabled();
    await userEvent.click(screen.getByRole('button', { name: /sonraki/i }));

    expect(onPageChange).toHaveBeenCalledWith(2);
  });

  it('tek sayfa varken butonları göstermez, hiç kayıt yokken hiçbir şey göstermez', () => {
    const { rerender, container } = render(
      <Pagination page={1} totalPages={1} totalCount={4} pageSize={10} onPageChange={vi.fn()} />,
    );
    expect(screen.queryByRole('button')).not.toBeInTheDocument();

    rerender(<Pagination page={1} totalPages={0} totalCount={0} pageSize={10} onPageChange={vi.fn()} />);
    expect(container).toBeEmptyDOMElement();
  });
});
