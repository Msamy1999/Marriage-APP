# نشر سَكينة مجانًا على MonsterASP.NET · Free deployment guide

سَكينة هو تطبيق **ASP.NET Core 8 + SQL Server**. الخطة التالية تنشره **مجانًا بالكامل** على
**MonsterASP.NET** (استضافة .NET مجانية + قاعدة بيانات MSSQL مجانية + نطاق فرعي + SSL، بدون بطاقة دفع).

Sakina is an **ASP.NET Core 8 + SQL Server** app. This guide deploys it **for free** on
**MonsterASP.NET** (free .NET hosting + free MSSQL DB + subdomain + SSL, no credit card).

---

## ما الذي جهّزناه مسبقًا · What's already prepared
- ✅ ترحيلات قاعدة البيانات تُطبَّق تلقائيًا عند الإقلاع (لا حاجة لتشغيلها يدويًا).
  Migrations auto-apply on startup — no manual DB step.
- ✅ يُزرع المشرفان تلقائيًا، **ويُزرع المستخدمون التجريبيون أيضًا** لأن `Seed:DemoData = true`.
  Both admins seed automatically, **and the demo grooms/brides seed too** because `Seed:DemoData = true`.
- ✅ مفاتيح حماية البيانات تُحفظ في `App_Data/keys` فتبقى الصور المشفّرة وجلسات الدخول صالحة بعد كل نشر.
  Data Protection keys persist to `App_Data/keys`, so encrypted photos and logins survive redeploys.

> الحسابات التجريبية في ملف [`DEMO-ACCOUNTS.md`](DEMO-ACCOUNTS.md). غيّر كلمات المرور قبل أي استخدام حقيقي.
> Demo logins are in [`DEMO-ACCOUNTS.md`](DEMO-ACCOUNTS.md). Change the passwords before real use.

---

## الخطوات · Steps

### 1) أنشئ حسابًا وموقعًا · Create an account + site
1. سجّل في **https://www.monsterasp.net** (مجاني).
2. من لوحة التحكم: **Create Website** → اختر الخطة المجانية. ستحصل على نطاق مثل `sakina.runasp.net`.
3. **Create Database** → اختر **MSSQL**. انسخ **سلسلة الاتصال (connection string)** التي يعطيها لك.

### 2) اضبط سلسلة الاتصال · Set the connection string
في لوحة MonsterASP.NET افتح موقعك → **Connection Strings** وأضف:
- **Name:** `DefaultConnection`
- **Value:** سلسلة اتصال MSSQL من الخطوة السابقة
- **Type:** SQL Server

> لا تضع سلسلة اتصال قاعدة بيانات الإنتاج داخل المستودع. اللوحة تحقنها تلقائيًا للتطبيق.
> Don't commit the production connection string. The panel injects it into the app at runtime.

(بديل · alternative: set an environment variable `ConnectionStrings__DefaultConnection`.)

### 3) ابنِ نسخة النشر · Publish the app
من جذر المشروع · from the repo root:
```bash
dotnet publish src/MarriageApp.Web -c Release -o publish
```
هذا يُنتج مجلد `publish/` جاهزًا للرفع (يحتوي على `web.config` لـ IIS تلقائيًا).
This produces a ready-to-upload `publish/` folder (it auto-includes the IIS `web.config`).

### 4) ارفع الملفات · Upload
استخدم إحدى الطريقتين من لوحة MonsterASP.NET:
- **Web Deploy:** نزّل ملف *Publish Profile* من اللوحة، واستوردْه في Visual Studio (**Publish → Import Profile**) ثم **Publish**. أسهل خيار.
- **FTP:** ارفع محتويات مجلد `publish/` إلى المجلد الجذري للموقع (عادةً `wwwroot` أو `/site`) عبر FileZilla ببيانات FTP من اللوحة.

### 5) شغّل وتحقّق · Browse & verify
1. افتح `https://your-site.runasp.net` — ستظهر الصفحة الرئيسية بالعربية.
2. أول طلب يطبّق الترحيلات ويزرع الحسابات (قد يتأخر بضع ثوانٍ).
3. سجّل الدخول كمشرفة: `admin@marriageapp.local` / `Admin#12345` → لوحة المشرف → «عرض أفضل التطابقات».

شارك الرابط `https://your-site.runasp.net` مع الناس لتجربة النظام. ✅

---

## ملاحظات وحلول · Notes & troubleshooting
- **إيقاف بيانات التجربة لاحقًا:** اضبط `Seed:DemoData` إلى `false` (في اللوحة كمتغير `Seed__DemoData=false` أو في `appsettings.json`) لمنع زرع المستخدمين التجريبيين.
- **خطأ 500.30 / لا يقلع:** غالبًا سلسلة الاتصال غير صحيحة — تأكد أن الاسم `DefaultConnection` وأن قاعدة MSSQL تعمل.
- **إعادة توجيه HTTPS:** الاستضافة توفّر SSL على النطاق الفرعي؛ إن واجهت حلقة إعادة توجيه، تأكد أن الموقع يُفتح عبر `https://`.
- **الصور:** نظام ملفات MonsterASP.NET دائم، فتبقى الصور المرفوعة ومفاتيح التشفير بعد إعادة التشغيل.
- **الأمان:** غيّر كلمات مرور المشرفين في `Seed:Admins`، واجعل `Seed:DemoData=false`، قبل أي إطلاق حقيقي.

## بديل سريع بدون نشر · Quick no-deploy alternative
لمشاركة فورية والتطبيق يعمل على جهازك:
```bash
cloudflared tunnel --url http://localhost:5279
```
يعطيك رابط `https://xxxxx.trycloudflare.com` تشاركه مباشرة (يجب بقاء جهازك يعمل).
