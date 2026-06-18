import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { TermsPage } from './TermsPage';

describe('TermsPage', () => {
  it('タイトルと利用規約の条文を描画する', () => {
    render(
      <MemoryRouter>
        <TermsPage />
      </MemoryRouter>,
    );
    expect(screen.getByText('利用規約')).toBeInTheDocument();
    expect(screen.getByText('Terms of Use')).toBeInTheDocument();
    expect(screen.getByText('第1条（掲載情報）')).toBeInTheDocument();
  });
});
