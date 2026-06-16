import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { ContactPage } from './ContactPage';

describe('ContactPage', () => {
  it('タイトルとメールリンクを描画する', () => {
    render(
      <MemoryRouter>
        <ContactPage />
      </MemoryRouter>,
    );
    expect(screen.getByText('お問い合わせ')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /support@ramen-zukan/ })).toBeInTheDocument();
  });
});
