# نشر المشروع على GitHub — دليل كامل خطوة بخطوة

**الهدف:** مستودع عام اسمه `kirademorty-optimizer` + إصدار `v1.2.0` فيه الملف التنفيذي، وملفات
المصدر ظاهرة للجميع. **الوقت المتوقع:** 15–20 دقيقة.

> هذا الدليل مكتوب لويندوز + PowerShell. الأوامر كلها تُنفَّذ داخل مجلد المشروع:
> `C:\Users\Administrator\Documents\Default Project\KiraDemorTY-Optimizer`
>
> **ملاحظة:** النسخة الحالية منشورة بالفعل على
> `https://github.com/KirademorTY21/kirademorty-optimizer`. هذا الدليل يوثّق ما تم، ويعيدك
> لنفس النتيجة من الصفر على أي جهاز آخر.

---

## 0) قبل أن تبدأ — 5 دقائق تحقّق

- [ ] عندك حساب GitHub. **اسم المستخدم المستهدف: `KirademorTY21`** لأن كل الروابط داخل البرنامج
      والـ README تشير إلى `github.com/KirademorTY21/kirademorty-optimizer`.
- [ ] إذا كان اسم المستخدم عندك مختلفًا، عدّل هذه الملفات قبل النشر:
      - `src/Brand.cs` → `GithubUrl` و `GithubLabel`
      - `README.md` (رابط المستودع في قسم الوثائق)
      - `docs/VIDEO-SCRIPT.md` (وصف الفيديو والتعليق المثبّت)
      ثم أعد البناء (`.\build.ps1`) قبل عمل الإصدار.
- [ ] **لا ترفع `dist\`** — الملف التنفيذي يجب أن يذهب إلى **Releases** فقط. `.gitignore` يستبعده
      أصلًا (`dist/` و `*.exe`).
- [ ] **لا ترفع بيانات جهازك**: `settings.ini` و `journal.tsv` و تقارير HTML الحقيقية. هذه ليست
      في المشروع، لكن راجع `git status` قبل أول commit.
- [ ] لا توجد مفاتيح أو كلمات مرور في المشروع (فحص سريع في الخطوة 2).

---

## 1) تثبيت Git (مرة واحدة)

```powershell
winget install --id Git.Git -e --source winget
```

**أغلق نافذة PowerShell وافتحها من جديد** (حتى يتحدّث `PATH`)، ثم:

```powershell
git --version
```

إذا لم يعمل `winget`، نزّل المثبّت من <https://git-scm.com/download/win> واختر الإعدادات
الافتراضية (Git from the command line + Windows Credential Manager).

---

## 2) فحص سريع قبل أول commit

```powershell
cd "C:\Users\Administrator\Documents\Default Project\KiraDemorTY-Optimizer"

# لا مسارات شخصية أو أسرار داخل الملفات التي سترفعها
Get-ChildItem -Recurse -File |
  Where-Object { $_.FullName -notmatch '\\(dist|\.git)\\' } |
  Select-String -Pattern 'C:\\Users\\|Password\s*=|api[_-]?key|Bearer ' -List
```

النتيجة المتوقعة: لا شيء. (أوامر الشرح داخل README تحتوي `C:\...` كأمثلة فقط، وهذا طبيعي —
راجع أي نتيجة وتأكد أنها ليست بياناتك.)

---

## 3) إنشاء المستودع محليًا وأول commit

```powershell
git init -b main

git config user.name  "KirademorTY21"
git config user.email "PUT-YOUR-GITHUB-EMAIL-HERE"

git add .
git status --short
```

**قبل الـ commit تحقّق من `git status`:**

- يجب ألّا يظهر `dist/` ولا أي `.exe` ولا `tools/*.exe`.
- يجب أن تظهر: `README.md`، `LICENSE`، `CHANGELOG.md`، `build.ps1`، `app.ico`، `app.manifest`،
  `src/`، `tools/` (ملفات `.cs` و `.ps1` فقط)، `docs/`، `.github/`.

```powershell
git commit -m "KiraDemorTY Optimizer v1.2.0"
```

---

## 4) إنشاء المستودع على GitHub

1. افتح <https://github.com/new>.
2. **Repository name:** `kirademorty-optimizer`
3. **Description:** `Open-source Windows latency & stutter tuner — 33 reversible tweaks, real before/after measurements.`
4. اختر **Public**.
5. **لا** تضف README / .gitignore / License — موجودة عندك.
6. اضغط **Create repository**.

---

## 5) ربط المستودع ودفع الكود

```powershell
git remote add origin https://github.com/KirademorTY21/kirademorty-optimizer.git
git push -u origin main
```

أول مرة سيفتح Git Credential Manager نافذة متصفح لتسجيل الدخول — سجّل بحسابك ووافق.
لا تحتاج Personal Access Token إذا استخدمت المتصفح.

**إذا ظهر خطأ `remote origin already exists`:**

```powershell
git remote set-url origin https://github.com/KirademorTY21/kirademorty-optimizer.git
```

---

## 6) إعدادات المستودع (5 دقائق، مهمة للثقة والظهور)

في صفحة المستودع:

| المكان | الإجراء |
|---|---|
| **About** (ترس بجانب الوصف) | الصق الوصف أعلاه + **Website:** `https://www.youtube.com/@KIRA-DEMORTY` |
| **Topics** | `windows`, `optimizer`, `stutter`, `latency`, `gaming`, `fps`, `open-source`, `csharp`, `winforms`, `windows-10`, `windows-11`, `performance` |
| **Settings → General → Features** | Issues ✅ · Discussions (اختياري) · Wiki ❌ · Projects ❌ |
| **Settings → General → Pull Requests** | اسمح بـ Squash merge فقط · فعّل Auto-delete branches |
| **Settings → General → Social preview** | ارفع `docs/thumbnail-1280x720.png` |
| **Security → Advisories** | تأكد أنه مفعّل (لتلقّي تقارير الثغرات بشكل خاص) |

> **ليش هذا مهم؟** الـ Topics هي اللي تخلي المشروع يظهر في بحث GitHub. وSocial preview هي
> الصورة اللي تظهر عند مشاركة الرابط في Discord/X — استخدم نفس صورة الفيديو للاتساق.

---

## 7) إنشاء الإصدار v1.2.0 ورفع الملف التنفيذي

### 7.1 ابنِ الملف من المصدر

```powershell
.\build.ps1
```

يجب أن ترى:

```
BUILD OK  ->  ...\dist\KiraDemorTY-Optimizer.exe
Size: 282 KB
Version: 1.2.0.0
```

### 7.2 احسب البصمة واكتب ملف التحقق

```powershell
$h = (Get-FileHash .\dist\KiraDemorTY-Optimizer.exe -Algorithm SHA256).Hash
"$h  KiraDemorTY-Optimizer.exe" | Set-Content .\dist\SHA256.txt -Encoding ASCII
Get-Content .\dist\SHA256.txt
```

> انشر البصمة في وصف الإصدار. هذا يعطي المستخدم طريقة يتأكد فيها أن الملف لم يُعدّل،
> حتى لو كان الملف غير موقّع رقميًا.

### 7.3 أنشئ الـ tag وارفعه

```powershell
git tag -a v1.2.0 -m "KiraDemorTY Optimizer 1.2.0"
git push origin v1.2.0
```

### 7.4 أنشئ الإصدار من الواجهة

1. **Releases** → **Draft a new release**.
2. اختر الـ tag: `v1.2.0` · **Release title:** `KiraDemorTY Optimizer v1.2.0`.
3. الصق ملاحظات `CHANGELOG.md` (قسم 1.2.0) في الوصف، وأضف في الآخر:

   ```
   SHA-256: <الصق البصمة>
   UnSigned build. Source code is public — build it yourself with build.ps1.
   ```

4. **Attach binaries:** `dist\KiraDemorTY-Optimizer.exe` و `dist\SHA256.txt`.
5. **Publish release**.

### بديل: GitHub CLI (اختياري)

```powershell
winget install --id GitHub.cli -e --source winget
# افتح طرفية جديدة
gh auth login

gh release create v1.2.0 `
  .\dist\KiraDemorTY-Optimizer.exe `
  .\dist\SHA256.txt `
  --title "KiraDemorTY Optimizer v1.2.0" `
  --notes-file .\docs\release-notes-1.2.0.md
```

> إن استخدمت `gh`، أنشئ `docs/release-notes-1.2.0.md` من قسم 1.2.0 في CHANGELOG أولًا.

---

## 8) تحقّق من بناء CI

اذهب إلى تبويب **Actions** → أحدث تشغيل لـ `build`:

- يجب أن يظهر ✅ على كل الخطوات (Build / Self test / Verify artifact / Upload).
- أول تشغيل قد يأخذ 2–4 دقائق.
- نزّل الـ artifact من أسفل الصفحة وتأكد أن حجمه قريب من 282 KB.

إذا فشل: افتح الخطوة الحمراء واقرأ آخر 20 سطرًا. الأسباب الشائعة:

| الخطأ | السبب | الحل |
|---|---|---|
| `csc.exe not found` | نسخة ويندوز في الـ runner مختلفة | لا يحدث على `windows-latest`؛ تأكد أنك لم تغيّر `runs-on` |
| فشل في `run-selftest.ps1` | اختبار يعتمد على جهازك | شغّله محليًا؛ لا تعتمد الاختبارات على أسماء/عتاد محدد |
| `Executable looks too small` | البناء لم يُنتج الملف | شغّل `.\build.ps1` محليًا واقرأ الخطأ |

---

## 9) التحقق النهائي — قائمة قبل نشر الفيديو

- [ ] الصفحة الرئيسية للمستودع تعرض README مع صور الواجهة.
- [ ] `Releases` تعرض `v1.2.0` وفيه `.exe` + `SHA256.txt`.
- [ ] رابط `https://github.com/KirademorTY21/kirademorty-optimizer/releases/latest` يفتح على الإصدار.
- [ ] Topics ظاهرة أسفل الوصف.
- [ ] رابط القناة في الـ About يعمل.
- [ ] **اختبار استنساخ نظيف** (أهم خطوة — تثبت أن المشروع قابل للبناء من الصفر):

  ```powershell
  cd $env:TEMP
  git clone https://github.com/KirademorTY21/kirademorty-optimizer.git kdt-clone
  cd kdt-clone
  .\build.ps1
  .\tools\run-selftest.ps1
  ```

  المتوقع: `passed 51, failed 0`.
- [ ] افتح التقرير من النسخة المستنسخة وتأكد أنه لا يحتوي اسم جهازك.
- [ ] جرّب تحميل الـ EXE من صفحة Releases على جهاز/مستخدم آخر إن أمكن.

---

## 10) دورة التحديث بعد اليوم (كرّرها لكل إصدار)

1. عدّل الكود.
2. ارفع رقم الإصدار في **مكانين**: `src/Brand.cs` (`Version`, `VersionTag`) و `app.manifest`
   (`assemblyIdentity version="X.Y.Z.0"`).
3. أضف قسمًا جديدًا في `CHANGELOG.md` (Added / Changed / Fixed).
4. حدّث `README.md` إذا تغيّر عدد التحسينات أو الفحوص أو الحجم.
5. `.\build.ps1` ثم `.\tools\run-selftest.ps1` (لازم 51/51 أو أكثر).
6. `git add .` → `git commit -m "..."` → `git push`.
7. `git tag -a vX.Y.Z -m "..."` → `git push origin vX.Y.Z`.
8. Releases → إصدار جديد: الـ EXE + SHA256 + ملاحظات.
9. اختبر رابط `releases/latest`، وحدّث وصف الفيديو إذا لزم.

---

## 11) حل المشاكل

| المشكلة | الحل |
|---|---|
| `git` غير معروف بعد التثبيت | أغلق كل نوافذ PowerShell وافتح واحدة جديدة |
| `Authentication failed` عند push | استخدم Git Credential Manager (المتصفح) بدل كلمة المرور |
| `GH001: Large files detected` | أنت تحاول رفع `.exe`؛ تأكد أن `.gitignore` يعمل (`git status` لا يعرضه) |
| الملفات العربية تظهر `????` في `git diff` | عرض فقط: `git config core.quotepath false`؛ الملفات نفسها UTF-8 سليمة |
| CI أخضر لكن الـ artifact قديم | نزّل من **أحدث** تشغيل، وتأكد أنه على `main` |
| اسم المستخدم مختلف عن الروابط | عدّل `Brand.cs` + README + سكربت الفيديو، أعد البناء، واعمل إصدارًا جديدًا |
| Windows SmartScreen يحذّر المستخدمين | متوقع للملفات غير الموقّعة؛ اذكر البصمة والكود المصدري في وصف الإصدار |
| تريد حذف المستودع لاحقًا | Settings → Danger Zone → Delete — لكن غيّر الروابط في البرنامج أولًا |

---

## 12) نصائح أمان وصيانة

- **لا ترفع أبدًا** `settings.ini` أو `journal.tsv` أو تقارير HTML حقيقية أو نسخًا احتياطية من الريجستري.
- فعّل **2FA** على حساب GitHub (Settings → Password and authentication).
- لا تضع بريدك الحقيقي في `git config` إذا لا تريد ظهوره في الـ commits — استخدم
  `YOURNAME@users.noreply.github.com` (تجده في GitHub → Settings → Emails).
- لا تقبل Pull Requests تعدّل `.github/workflows/` من حسابات مجهولة دون مراجعة (سطح هجوم CI).
- راجع `SECURITY.md` واجعله ظاهرًا؛ فهو يجعل تقارير الثغرات خاصة بدل Issues عامة.
- احتفظ بنسخة من `SHA256.txt` لكل إصدار؛ المستخدمون يستخدمونها للتحقق.
- إذا أضفت مكتبات خارجية لاحقًا، فعّل Dependabot (Settings → Code security).

---

**بعد الانتهاء:** افتح الفيديو وسجّل لقطة صفحة الـ Releases الحقيقية — مصداقية أعلى من صورة
مُعدّلة، وتُظهر أن التحميل بضغطة واحدة.
