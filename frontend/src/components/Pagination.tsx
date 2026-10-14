type PaginationProps = {
  page: number;
  totalPages: number;
  totalCount: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  disabled?: boolean;
};

export function Pagination({ page, totalPages, totalCount, pageSize, onPageChange, disabled }: PaginationProps) {
  if (totalCount === 0) return null;

  const from = (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, totalCount);

  return (
    <nav className="pagination" aria-label="Sayfalama">
      <span className="pagination-info" aria-live="polite">
        {from}–{to} / {totalCount} görev
      </span>
      {totalPages > 1 && (
        <div className="pagination-buttons">
          <button
            type="button"
            className="btn-secondary"
            onClick={() => onPageChange(page - 1)}
            disabled={disabled || page <= 1}
          >
            ‹ Önceki
          </button>
          <span className="pagination-page">
            Sayfa {page} / {totalPages}
          </span>
          <button
            type="button"
            className="btn-secondary"
            onClick={() => onPageChange(page + 1)}
            disabled={disabled || page >= totalPages}
          >
            Sonraki ›
          </button>
        </div>
      )}
    </nav>
  );
}
