# بيانات الدخول التجريبية — سَكينة · Demo Accounts

> صفحة الدخول · Login page: **`/Account/Login`**
> محليًا · locally: http://localhost:5279/Account/Login

## 👤 المشرفون · Admins

| الدور · Role | البريد · Email | كلمة المرور · Password | ملاحظات · Notes |
|---|---|---|---|
| 👩 مشرفة (أنثى) · Female admin | `admin@marriageapp.local` | `Admin#12345` | ترى وتُنزّل صور العرائس، وتكشفها للعريس · sees/downloads women's photos & reveals them to the groom |
| 👨 مشرف (ذكر) · Male admin | `admin.male@marriageapp.local` | `Admin#12345` | كل الصلاحيات عدا صور العرائس · everything except women's photos |

بعد تسجيل دخول المشرف يُوجَّه إلى **لوحة المشرف** → «عرض أفضل التطابقات».
After admin login you land on the **Admin dashboard** → "Top matches".

## 🧑‍🤝‍🧑 المستخدمون العاديون · Normal users

| النوع · Type | البريد · Email | كلمة المرور · Password |
|---|---|---|
| 🤵 عريس (ذكر) · Groom (male) | `ahmed.groom@test.local` | `Test#12345` |
| 👰 عروسة (أنثى) · Bride (female) | `fatma.bride@test.local` | `Test#12345` |

بعد تسجيل دخول المستخدم يُوجَّه إلى **لوحته الشخصية** (الحالة + الصور).
After user login you land on the personal **dashboard** (status + photos).

---

### مستخدمون تجريبيون إضافيون · Extra seeded users (all password `Test#12345`)
- العرسان · Grooms: `omar.groom@test.local`, `mostafa.groom@test.local`, `youssef.groom@test.local`
- العرائس · Brides: `aisha.bride@test.local`, `mariam.bride@test.local`, `nour.bride@test.local`, `salma.bride@test.local`, `hagar.bride@test.local`

> ⚠️ هذه حسابات تجريبية بكلمات مرور معروفة — غيّرها قبل أي استخدام حقيقي.
> These are demo accounts with known passwords — change them before any real use.
> (المشرفون يُضبطون في `appsettings.json` ← `Seed:Admins`، والمستخدمون التجريبيون في `TestDataSeeder.cs` ويُزرعون في وضع `Development` فقط.)
