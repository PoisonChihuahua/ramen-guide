import { test, expect, type Page } from '@playwright/test';

// バックエンド API のオリジン（VITE_API_BASE_URL の既定値）。
// dev サーバの /src/api/*.ts と取り違えないよう、オリジンを明示してモックする。
const API = 'http://localhost:5105';

const USER = { id: 1, email: 'taro@example.com', displayName: '太郎' };
const AUTH_BODY = JSON.stringify({
  token: 'access-token',
  refreshToken: 'refresh-token',
  user: USER,
});

/** 店舗一覧 API は常に空配列を返し、ホーム描画を安定させる。 */
async function mockShops(page: Page) {
  await page.route(`${API}/api/shops**`, (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: '[]' }),
  );
}

function jsonRoute(status: number, body: string) {
  return (route: import('@playwright/test').Route) =>
    route.fulfill({ status, contentType: 'application/json', body });
}

test.beforeEach(async ({ page }) => {
  await mockShops(page);
});

test('未ログインで /mypage にアクセスすると /login にリダイレクトされる', async ({
  page,
}) => {
  await page.goto('/mypage');

  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole('heading', { name: 'ログイン' })).toBeVisible();
});

test('ログインに成功するとホームに遷移し、マイページを閲覧できる', async ({
  page,
}) => {
  await page.route(`${API}/api/auth/login`, jsonRoute(200, AUTH_BODY));
  await page.route(`${API}/api/auth/me`, jsonRoute(200, JSON.stringify(USER)));

  await page.goto('/login');
  await page.getByLabel('メールアドレス').fill('taro@example.com');
  await page.getByLabel('パスワード').fill('password123');
  await page.getByRole('button', { name: 'ログイン' }).click();

  // ホームに戻り、ヘッダーがログイン状態になる
  await expect(page).toHaveURL(/\/$/);
  await expect(page.getByText('太郎 さん')).toBeVisible();

  // 保護されたマイページを閲覧できる
  await page.getByRole('link', { name: 'マイページ' }).click();
  await expect(page).toHaveURL(/\/mypage$/);
  await expect(page.getByRole('heading', { name: 'マイページ' })).toBeVisible();
  await expect(page.getByText('taro@example.com')).toBeVisible();
});

test('ログインに失敗するとエラーメッセージを表示する', async ({ page }) => {
  await page.route(
    `${API}/api/auth/login`,
    jsonRoute(
      401,
      JSON.stringify({
        message: 'メールアドレスまたはパスワードが正しくありません。',
      }),
    ),
  );

  await page.goto('/login');
  await page.getByLabel('メールアドレス').fill('taro@example.com');
  await page.getByLabel('パスワード').fill('wrong-pass');
  await page.getByRole('button', { name: 'ログイン' }).click();

  await expect(
    page.getByText('メールアドレスまたはパスワードが正しくありません。'),
  ).toBeVisible();
  await expect(page).toHaveURL(/\/login$/);
});

test('新規登録に成功するとホームに遷移する', async ({ page }) => {
  await page.route(`${API}/api/auth/register`, jsonRoute(200, AUTH_BODY));

  await page.goto('/register');
  await page.getByLabel('表示名').fill('太郎');
  await page.getByLabel('メールアドレス').fill('taro@example.com');
  await page.getByLabel('パスワード（8文字以上）').fill('password123');
  await page.getByRole('button', { name: '登録する' }).click();

  await expect(page).toHaveURL(/\/$/);
  await expect(page.getByText('太郎 さん')).toBeVisible();
});

test('ログアウトすると保護ページにアクセスできなくなる', async ({ page }) => {
  await page.route(`${API}/api/auth/login`, jsonRoute(200, AUTH_BODY));
  await page.route(`${API}/api/auth/me`, jsonRoute(200, JSON.stringify(USER)));
  await page.route(`${API}/api/auth/logout`, (route) =>
    route.fulfill({ status: 204, body: '' }),
  );

  await page.goto('/login');
  await page.getByLabel('メールアドレス').fill('taro@example.com');
  await page.getByLabel('パスワード').fill('password123');
  await page.getByRole('button', { name: 'ログイン' }).click();
  await expect(page.getByText('太郎 さん')).toBeVisible();

  // ヘッダーのログアウトを押す（フッターにも同名リンクがあるためヘッダーnavに限定）
  const headerNav = page.getByRole('navigation', { name: 'メイン' });
  await page.getByRole('button', { name: 'ログアウト' }).click();
  await expect(headerNav.getByRole('link', { name: 'ログイン' })).toBeVisible();

  // 保護ページは再びリダイレクトされる
  await page.goto('/mypage');
  await expect(page).toHaveURL(/\/login$/);
});
