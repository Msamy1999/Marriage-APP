using System.ComponentModel.DataAnnotations;

namespace MarriageApp.Core.Enums;

/// <summary>الجنس — used as the discriminator on the unified Profile table.</summary>
public enum Gender
{
    [Display(Name = "عريس")] Male = 1,
    [Display(Name = "عروسة")] Female = 2
}

/// <summary>الحالة الاجتماعية.</summary>
public enum MaritalStatus
{
    [Display(Name = "أعزب/آنسة")] Single = 1,
    [Display(Name = "مطلق/مطلقة")] Divorced = 2,
    [Display(Name = "أرمل/أرملة")] Widowed = 3
}

/// <summary>
/// المؤهل التعليمي — ordered ascending so the matching algorithm can give
/// partial credit for an adjacent level when the exact one is not selected.
/// </summary>
public enum EducationLevel
{
    [Display(Name = "أقل من الثانوية")] BelowSecondary = 1,
    [Display(Name = "ثانوي / دبلوم")] Secondary = 2,
    [Display(Name = "معهد / دبلوم عالي")] Diploma = 3,
    [Display(Name = "جامعي")] University = 4,
    [Display(Name = "دراسات عليا (ماجستير/دكتوراه)")] Postgraduate = 5
}

/// <summary>
/// مستوى الالتزام الديني — ordered from lightest to strongest commitment.
/// </summary>
public enum ReligiousCommitment
{
    [Display(Name = "عدم الاختلاط")] NoMixing = 1,
    [Display(Name = "سماع دروس")] ListensToLessons = 2,
    [Display(Name = "طلب علم")] SeeksKnowledge = 3,
    [Display(Name = "حفظ القرآن")] MemorizesQuran = 4
}

/// <summary>شكل الملابس (للعروسة).</summary>
public enum DressCode
{
    [Display(Name = "منتقبة")] Niqab = 1,
    [Display(Name = "عبايات")] Abaya = 2,
    [Display(Name = "جيب وفساتين")] SkirtsAndDresses = 3,
    [Display(Name = "بنطلونات")] Trousers = 4
}

/// <summary>مدى التزام الأسرة.</summary>
public enum FamilyCommitmentLevel
{
    [Display(Name = "غير ملتزمة")] NotCommitted = 1,
    [Display(Name = "ملتزمة إلى حد ما")] SomewhatCommitted = 2,
    [Display(Name = "ملتزمة")] Committed = 3,
    [Display(Name = "ملتزمة جدًا")] VeryCommitted = 4
}

/// <summary>حالة ملف المتقدم.</summary>
public enum ProfileStatus
{
    [Display(Name = "غير مكتمل")] Incomplete = 0,
    [Display(Name = "قيد المطابقة")] PendingMatch = 1,
    [Display(Name = "تمت المطابقة")] Matched = 2,
    [Display(Name = "موقوف")] Suspended = 3
}

/// <summary>مراحل سير عمل المطابقة التي يديرها المشرف.</summary>
public enum MatchStatus
{
    [Display(Name = "مقترح من النظام")] Pending = 0,
    [Display(Name = "تم ترشيحه")] ShortlistedByAdmin = 1,
    [Display(Name = "تمت الموافقة")] Approved = 2,
    [Display(Name = "تم التواصل")] Contacted = 3,
    [Display(Name = "تم القبول")] Accepted = 4,
    [Display(Name = "تم الرفض")] Rejected = 5
}

/// <summary>سياسة ظهور الصور (يحددها صاحب الملف كحد أقصى للوصول).</summary>
public enum PhotoVisibility
{
    [Display(Name = "مخفية تمامًا")] Hidden = 0,
    [Display(Name = "للمشرف فقط")] AdminOnly = 1,
    [Display(Name = "بعد موافقة المشرف على التطابق")] AfterMatchApproval = 2
}

/// <summary>قناة الإشعار.</summary>
public enum NotificationChannel
{
    [Display(Name = "داخل التطبيق")] InApp = 1,
    [Display(Name = "البريد الإلكتروني")] Email = 2,
    [Display(Name = "رسالة نصية")] Sms = 3,
    [Display(Name = "واتساب")] WhatsApp = 4
}

/// <summary>نوع الوصول إلى الصورة (للتدقيق).</summary>
public enum PhotoAccessAction
{
    [Display(Name = "عرض")] View = 1,
    [Display(Name = "تنزيل")] Download = 2,
    [Display(Name = "رفض الوصول")] Denied = 3
}
