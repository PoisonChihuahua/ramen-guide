import { describe, it, expect, beforeEach } from 'vitest';
import { readFavorites, writeFavorites } from './favorites';

describe('favorites storage', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('未保存のときは空配列を返す', () => {
    expect(readFavorites()).toEqual([]);
  });

  it('書き込んだ ID 配列を読み戻せる', () => {
    writeFavorites([3, 1, 2]);

    expect(readFavorites()).toEqual([3, 1, 2]);
  });

  it('不正な JSON は空配列にフォールバックする', () => {
    localStorage.setItem('ramensite_favorites', '{not-json');

    expect(readFavorites()).toEqual([]);
  });

  it('数値以外の要素は除外する', () => {
    localStorage.setItem('ramensite_favorites', JSON.stringify([1, 'x', 2, null]));

    expect(readFavorites()).toEqual([1, 2]);
  });
});
