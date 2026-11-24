## 1. sessionStorage、localStorage 與 Cookie 的差異

| 比較項目 | `sessionStorage` | `localStorage` | Cookie |
| --- | --- | --- | --- |
| 儲存位置 | 瀏覽器 | 瀏覽器 | 瀏覽器 |
| 保存期限 | 分頁或瀏覽器分頁關閉後清除 | 除非主動刪除，否則會持續保存 | 依 `Expires` 或 `Max-Age` 決定；未設定時通常是工作階段 Cookie |
| 可用範圍 | 僅限建立資料的分頁及相同 Origin | 相同 Origin 的所有分頁 | 由 `Domain`、`Path`、`SameSite` 等屬性決定 |
| 是否自動傳送至後端 | 不會，必須由前端程式自行加入請求 | 不會，必須由前端程式自行加入請求 | 符合 Cookie 條件時，瀏覽器會自動隨 HTTP 請求傳送 |
| JavaScript 是否可讀取 | 可以 | 可以 | 預設可以；設定 `HttpOnly` 後不可讀取 |
| 常見容量 | 約 5 MB，依瀏覽器而異 | 約 5 MB，依瀏覽器而異 | 每個 Cookie 約 4 KB，瀏覽器也會限制數量 |
| 主要安全風險 | 遭遇 XSS 時可能被讀取 | 遭遇 XSS 時可能被讀取，且資料長期存在 | 自動隨請求傳送，需注意 CSRF；未設定 `HttpOnly` 時也可能遭 XSS 讀取 |
| 常見用途 | 單一分頁內的暫存資料、表單進度 | 使用者偏好、非敏感且需長期保存的資料 | 登入工作階段、Refresh Token、伺服器需要接收的少量資料 |

### 使用建議

- 敏感憑證不建議存放在 `localStorage` 或 `sessionStorage`，因為 JavaScript 可以直接讀取，遭遇 XSS 時容易外洩。
- Refresh Token 適合存放在設定了 `HttpOnly`、`Secure` 及合適 `SameSite` 的 Cookie。
- Cookie 設定 `HttpOnly` 只能防止 JavaScript 讀取，不能單獨防止 CSRF；仍需搭配 `SameSite`、CSRF Token 或來源檢查。
- Access Token 若存放於前端記憶體中，可降低長期保存造成的風險，但重新整理頁面後通常需要透過 Refresh Token 重新取得。


## 2. Cookie 的 5 個屬性說明：HttpOnly、Secure、SameSite、Expires、Path

| 屬性 | 功能 | 常見設定 | 注意事項 |
| --- | --- | --- | --- |
| `HttpOnly` | 禁止前端 JavaScript 透過 `document.cookie` 讀取 Cookie | `HttpOnly = true` | 可降低 Cookie 被 XSS 竊取的風險，但不能防止瀏覽器自動攜帶 Cookie 發出惡意請求 |
| `Secure` | 只允許瀏覽器透過 HTTPS 傳送 Cookie | `Secure = true` | 正式環境應設為 `true`；若網站使用一般 HTTP，Cookie 通常不會被傳送 |
| `SameSite` | 控制跨網站請求是否可以攜帶 Cookie，主要用於降低 CSRF 風險 | `Strict`、`Lax`、`None` | `None` 允許跨站傳送，但必須同時設定 `Secure = true` |
| `Expires` | 設定 Cookie 的到期時間 | `Expires = DateTimeOffset.UtcNow.AddMinutes(20)` | 到期後瀏覽器會移除 Cookie；若未設定，通常會成為關閉瀏覽器工作階段後失效的 Session Cookie |
| `Path` | 限制 Cookie 可以被傳送到哪些 URL 路徑 | `Path = "/jwtTestApi"` | `/jwtTestApi` 與其子路徑會攜帶 Cookie，但 `/api` 不會；它是 URL 路徑，不是前端路由或後端實體資料夾 |

### SameSite 模式比較

| 模式 | 跨站請求攜帶 Cookie | 適合情境 |
| --- | --- | --- |
| `Strict` | 幾乎完全禁止 | 安全要求較高，且不需要跨站流程的系統 |
| `Lax` | 部分允許，例如使用者透過頂層導覽進入網站 | 一般網站與多數登入工作階段 |
| `None` | 允許 | 前後端位於不同 Site，且確實需要跨站 Cookie 的情境；必須搭配 `Secure` |

### 同域網站舉例

在討論 Cookie 與 `SameSite` 時，「同域」容易與「同源（Same Origin）」混淆。瀏覽器主要會區分以下概念：

| 類型 | 網址 A | 網址 B | 說明 |
| --- | --- | --- | --- |
| 同源且同站 | `https://www.example.com/page` | `https://www.example.com/api` | 通訊協定、主機名稱及 Port 都相同，只有 URL 路徑不同 |
| 不同源但同站 | `https://app.example.com` | `https://api.example.com` | 子網域不同，所以不是同源；但註冊網域與通訊協定相同，因此通常屬於同站 |
| 不同源但同站 | `https://localhost:5173` | `https://localhost:7044` | Port 不同，所以不是同源；但 `SameSite` 判斷通常不包含 Port，因此屬於同站 |
| 跨站 | `https://www.example.com` | `https://www.example.net` | 註冊網域不同 |
| 跨站 | `https://app.example.com` | `http://api.example.com` | 即使網域相同，通訊協定不同，在現代瀏覽器的 Schemeful Same-Site 規則下仍屬於跨站 |

同源的判斷包含以下三個部分，任一項不同就是不同源：

1. 通訊協定，例如 `http` 或 `https`
2. 主機名稱，例如 `app.example.com`
3. Port，例如 `5173` 或 `7044`

## 3. 使用 Access Token 及 Refresh Token
```text
1. 使用者提交帳號與密碼
                ↓
2. 後端驗證登入資料
                ↓
3. 後端回傳 Access Token
   並將 Refresh Token 寫入 HttpOnly Cookie
                ↓
4. 前端使用 Access Token 呼叫受保護的 API
                ↓
5. Access Token 過期，API 回傳 401
                ↓
6. 前端呼叫 Refresh API，瀏覽器自動攜帶 Refresh Token Cookie
                ↓
7. 後端驗證 Refresh Token，回傳新的 Access Token
   並建議同時輪替 Refresh Token
                ↓
8. 前端使用新的 Access Token 重新呼叫原本的 API
```

### 使用 Access Token 呼叫 API

前端取得 Access Token 後，通常將它放在 `Authorization` Header：

```javascript
const response = await fetch(
  "https://localhost:7044/jwtTestApi/jwt/secure",
  {
    headers: {
      Authorization: `Bearer ${accessToken}`
    }
  }
);
```

後端可使用 `[Authorize]` 保護 API：

```csharp
[Authorize]
[HttpGet("secure")]
public IActionResult Secure()
{
    return Ok();
}
```

### 使用 Refresh Token 更新 Access Token

Refresh Token 如果存放於 `HttpOnly` Cookie，前端 JavaScript 不需要也無法直接讀取它。前端只需呼叫 Refresh API：

```javascript
const response = await fetch(
  "https://localhost:7044/jwtTestApi/jwt/refreshAccessToken",
  {
    method: "POST",
    credentials: "include"
  }
);

if (!response.ok) {
  // Refresh Token 已失效，要求使用者重新登入。
}

const accessToken = await response.text();
```

如果前端與後端不同源，登入和更新 Token 的請求都要設定 `credentials: "include"`，後端 CORS 也必須指定允許的來源並呼叫 `AllowCredentials()`。

### Refresh Token Rotation

每次使用 Refresh Token 時，建議同時簽發新的 Refresh Token，並讓舊的 Refresh Token 失效，這種作法稱為 Refresh Token Rotation：

```text
Refresh Token A → 換取新的 Access Token 與 Refresh Token B
Refresh Token A → 立即失效，不可再次使用
```

如果已失效的 Refresh Token A 再次出現，可能表示 Token 已被竊取。此時可以撤銷該使用者整個登入工作階段，並要求重新登入。

### 登出與撤銷

登出不應只刪除前端保存的 Access Token，還應：

1. 在伺服器端撤銷 Refresh Token。
2. 刪除 Refresh Token Cookie。
3. 清除前端記憶體中的 Access Token。

刪除 Cookie 時，`Path` 和 `Domain` 必須與建立 Cookie 時一致：

```csharp
Response.Cookies.Delete("refresh_20261123_token", new CookieOptions
{
    Path = "/jwtTestApi"
});
```

### 安全注意事項

- Access Token 與 Refresh Token 應使用不同用途及權限，不能將 Refresh Token 當成一般 API 的 Access Token。
- Access Token 的角色應是一般 API 所需角色；Refresh Token 則只應具有呼叫 Refresh API 所需的權限。
- 不建議將長效 Refresh Token 存入 `localStorage` 或 `sessionStorage`，以降低遭 XSS 讀取的風險。
- Refresh API 會簽發 Token 並改變驗證狀態，建議使用 `POST`，不要使用 `GET`。
- 使用 Cookie 傳送 Refresh Token 時仍需防範 CSRF，可搭配 `SameSite`、CSRF Token 及 `Origin`／`Referer` 檢查。
- JWT 簽章金鑰應持久保存並妥善管理；若應用程式每次啟動都重新產生金鑰，重啟後所有既有 Token 都會失效。
- Refresh Token 過期、遭撤銷或驗證失敗時，應回傳 `401 Unauthorized`，前端再引導使用者重新登入。
