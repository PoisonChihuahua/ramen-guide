import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { StarRating, StarRatingInput } from './StarRating';

describe('StarRating', () => {
  it('評価値と件数をアクセシブルラベルに含める', () => {
    render(<StarRating value={4.2} count={7} />);

    expect(
      screen.getByRole('img', {
        name: '5段階評価で 4.2（7件のレビュー）',
      }),
    ).toBeInTheDocument();
    expect(screen.getByText('（7件）')).toBeInTheDocument();
  });

  it('範囲外の値は0〜5にクランプする', () => {
    render(<StarRating value={9} />);

    expect(
      screen.getByRole('img', { name: '5段階評価で 5.0' }),
    ).toBeInTheDocument();
  });
});

describe('StarRatingInput', () => {
  it('星をクリックすると onChange にその値を渡す', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<StarRatingInput value={0} onChange={onChange} />);

    await user.click(screen.getByRole('radio', { name: '星4つ' }));

    expect(onChange).toHaveBeenCalledWith(4);
  });

  it('現在値の星が aria-checked になる', () => {
    render(<StarRatingInput value={3} onChange={() => {}} />);

    expect(screen.getByRole('radio', { name: '星3つ' })).toBeChecked();
    expect(screen.getByRole('radio', { name: '星2つ' })).not.toBeChecked();
  });
});
