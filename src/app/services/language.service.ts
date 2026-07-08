import { Injectable, signal, effect } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class LanguageService {
  currentLang = signal<'en' | 'ar'>('en');

  // Bilingual translation dictionary
  private dict: Record<string, Record<'en' | 'ar', string>> = {
    // Shared Navigation
    'brandName': { en: 'Luminus Giving', ar: 'لومينوس للعطاء' },
    'initiative': { en: 'Donation Portal', ar: 'بوابة التبرع بالدم' },
    'feed': { en: 'Active Feed', ar: 'المنصة النشطة' },
    'screening': { en: 'Screening Wizard', ar: 'فحص الأهلية' },
    'dashboard': { en: 'My Dashboard', ar: 'لوحتي الشخصية' },
    'adminPanel': { en: 'Admin Panel', ar: 'لوحة التحكم للمشرف' },
    'signOut': { en: 'Sign Out', ar: 'تسجيل الخروج' },
    'notifications': { en: 'Notifications', ar: 'التنبيهات الإدارية' },
    'noNotifications': { en: 'No new alerts', ar: 'لا توجد تنبيهات جديدة' },

    // Login & Register Pages
    'welcome': { en: 'Welcome Back', ar: 'مرحباً بك مجدداً' },
    'portalSubtitle': { en: 'Luminus Giving Donation Portal', ar: 'بوابة التبرع بالدم لمبادرة لومينوس للعطاء' },
    'email': { en: 'Email Address', ar: 'البريد الإلكتروني' },
    'password': { en: 'Password', ar: 'كلمة المرور' },
    'signIn': { en: 'Sign In', ar: 'تسجيل الدخول' },
    'noAccount': { en: "Don't have an account?", ar: 'ليس لديك حساب؟' },
    'createAccount': { en: 'Create account', ar: 'إنشاء حساب جديد' },
    'createDonorAccount': { en: 'Create Donor Account', ar: 'إنشاء حساب متبرع جديد' },
    'joinNetwork': { en: "Join Jordan's Blood Network", ar: 'انضم إلى الشبكة الوطنية للتبرع بالدم في الأردن' },
    'fullName': { en: 'Full Name (as in National ID)', ar: 'الاسم الكامل (كما هو في الهوية الشخصية)' },
    'nationality': { en: 'Nationality', ar: 'الجنسية' },
    'jordanian': { en: 'Jordanian', ar: 'أردني' },
    'other': { en: 'Other / Non-Jordanian', ar: 'جنسية أخرى / غير أردني' },
    'nationalId': { en: 'National ID', ar: 'الرقم الوطني' },
    'nationalIdPlaceholder': { en: '10-digit National ID', ar: 'الرقم الوطني المكون من 10 أرقام' },
    'notRequiredNonJordanian': { en: 'Not required for Non-Jordanians', ar: 'غير مطلوب لغير الأردنيين' },
    'nationalIdHint': { en: 'Must be exactly 10 digits (Jordanian verification)', ar: 'يجب أن يتكون من 10 أرقام (للأردنيين)' },
    'mobile': { en: 'Mobile Number', ar: 'رقم الهاتف المحمول' },
    'dob': { en: 'Date of Birth', ar: 'تاريخ الميلاد' },
    'dobHint': { en: 'Age must be 18 to 65 to be eligible to book', ar: 'يجب أن يتراوح العمر بين 18 و 65 عاماً لتكون مؤهلاً للحجز' },
    'gender': { en: 'Gender', ar: 'الجنس' },
    'male': { en: 'Male', ar: 'ذكر' },
    'female': { en: 'Female', ar: 'أنثى' },
    'noticeNationality': { en: 'Notice: Organization rules restrict non-Jordanian users from booking appointments. Registering with "Other" nationality will mark your account as permanently ineligible to donate blood under local regulatory structures.', ar: 'ملاحظة: تمنع القوانين المحلية والتعليمات التنظيمية غير الأردنيين من حجز مواعيد التبرع بالدم. اختيار جنسية أخرى سيعلم حسابك كغير مؤهل للتبرع بالدم بشكل دائم.' },
    'alreadyHaveAccount': { en: 'Already have an account?', ar: 'لديك حساب بالفعل؟' },

    // Screening Wizard
    'screeningTitle': { en: 'Digital Donation Screening Form', ar: 'نموذج فحص الأهلية الرقمي للتبرع بالدم' },
    'screeningSubtitle': { en: 'Explore current blood donation events and urgent emergency requests in Jordan.', ar: 'الاستبيان الطبي الرقمي المعتمد في الأردن لتقييم الأهلية قبل التبرع بالدم.' },
    'step': { en: 'Step', ar: 'الخطوة' },
    'step1Title': { en: 'General Guidelines & Criteria', ar: 'الإرشادات العامة والمعايير الأساسية' },
    'step2Title': { en: 'Screening Questionnaire', ar: 'الاستبيان الطبي السريري' },
    'step3Title': { en: 'Vital Screening & Medical Details', ar: 'العلامات الحيوية والتفاصيل المخبرية' },
    'step4Title': { en: 'Final Confirmation & Review', ar: 'التأكيد النهائي والمراجعة' },
    'generalGuidelines': { en: 'General Blood Donation Guidelines', ar: 'إرشادات التبرع بالدم العامة في الأردن' },
    'criteriaAge': { en: 'Age: 18 - 65 years old.', ar: 'العمر: بين 18 و 65 عاماً.' },
    'criteriaWeight': { en: 'Weight: Minimum 50 kg.', ar: 'الوزن: 50 كغم كحد أدنى.' },
    'criteriaInterval': { en: 'Donation Interval: Minimum 90 days since last donation.', ar: 'الفترة الفاصلة: 90 يوماً على الأقل منذ آخر عملية تبرع بالدم.' },
    'criteriaHealth': { en: 'Health: Must feel well and not suffer from infectious diseases.', ar: 'الحالة الصحية: يجب أن تشعر بصحة جيدة وخلو الجسم من الأمراض المعدية.' },
    'startScreening': { en: 'Start Screening Wizard', ar: 'البدء بفحص الأهلية' },
    'qTattoo': { en: 'Have you had any tattoos or piercings in the past 6 months?', ar: 'هل قمت برسم أي وشم أو ثقب في الجسم (بيرسينج) خلال الـ 6 أشهر الماضية؟' },
    'qMeds': { en: 'Are you currently taking any antibiotics or target medications?', ar: 'هل تتناول حالياً أي مضادات حيوية أو أدوية معينة؟' },
    'qSurgery': { en: 'Have you undergone any major surgery in the past 6 months?', ar: 'هل خضعت لأي عملية جرافية كبرى خلال الـ 6 أشهر الماضية؟' },
    'qChronic': { en: 'Do you suffer from any chronic cardiovascular or blood conditions?', ar: 'هل تعاني من أي أمراض مزمنة في القلب أو الأوعية الدموية أو أمراض الدم؟' },
    'qPreg': { en: 'Are you currently pregnant or breastfeeding?', ar: 'هل أنتِ حامل أو مرضعة حالياً؟' },
    'unknownCheck': { en: "I don't know my Hemoglobin or Hematocrit levels (verify on arrival)", ar: 'لا أعرف مستويات الهيموغلوبين أو الهيماتوكريت الخاصة بي (سيتم فحصها عند الوصول)' },
    'hemoglobin': { en: 'Hemoglobin Level (g/dL)', ar: 'مستوى الهيموغلوبين (غرام/ديسيلتر)' },
    'hematocrit': { en: 'Hematocrit Level (%)', ar: 'مستوى الهيماتوكريت (%)' },
    'weight': { en: 'Weight (kg)', ar: 'الوزن (كغم)' },
    'bloodGroup': { en: 'Blood Group', ar: 'فصيلة الدم' },
    'vitalsDisclaimer': { en: 'Disclaimer: Vitals will be verified by medical staff at the center on arrival.', ar: 'تنبيه: سيتم التحقق من العلامات الحيوية وإجراء الفحص الطبي الدقيق من قبل الفريق الطبي بالمركز عند وصولك.' },
    'eligibilityStatus': { en: 'Eligibility Status', ar: 'حالة الأهلية الطبية للتبرع' },
    'eligible': { en: 'Eligible', ar: 'مؤهل للتبرع' },
    'ineligible': { en: 'Ineligible', ar: 'غير مؤهل للتبرع' },
    'pendingReview': { en: 'Pending Review (Vitals Check on Arrival)', ar: 'قيد المراجعة (فحص الحيوية في المركز)' },
    'screeningStatement': { en: 'I declare that all answers are true and accurate to the best of my knowledge.', ar: 'أقر بأن جميع الإجابات المقدمة صحيحة ودقيقة وخالية من التضليل.' },
    'submitForm': { en: 'Submit Screening Form', ar: 'تقديم نموذج الفحص الطبي' },
    'next': { en: 'Next Step', ar: 'الخطوة التالية' },
    'back': { en: 'Previous Step', ar: 'الخطوة السابقة' },
    'yes': { en: 'Yes', ar: 'نعم' },
    'no': { en: 'No', ar: 'لا' },

    // Active Feed Page
    'searchLocation': { en: 'Search location or title...', ar: 'ابحث عن الموقع أو عنوان الحملة...' },
    'allPostTypes': { en: 'All Post Types', ar: 'جميع أنواع الإعلانات' },
    'donationEvents': { en: 'Donation Events', ar: 'حملات التبرع' },
    'emergencyRequests': { en: 'Emergency Requests', ar: 'نداءات الطوارئ العاجلة' },
    'anyBloodType': { en: 'Any Blood Type', ar: 'أي فصيلة دم' },
    'resetFilters': { en: 'Reset Filters', ar: 'إعادة تعيين التصفية' },
    'activeFeedTitle': { en: 'Active Donation Feed', ar: 'منصة التبرعات الفعالة' },
    'activeFeedSubtitle': { en: 'Explore current blood donation events and urgent emergency requests in Jordan.', ar: 'استكشف حملات التبرع بالدم الميدانية ونداءات التبرع العاجلة في مستشفيات الأردن.' },
    'donationEventTag': { en: 'Donation Event', ar: 'حملة تبرع بالدم' },
    'emergencyRequestTag': { en: 'Emergency Request', ar: 'نداء طوارئ عاجل' },
    'openCapacity': { en: 'Open Capacity', ar: 'سعة تبرع مفتوحة' },
    'bookAppointment': { en: 'Book Appointment', ar: 'حجز موعد تبرع' },
    'expires': { en: 'Expires:', ar: 'ينتهي في:' },
    'callCenter': { en: 'Call Center', ar: 'اتصال هاتفي' },
    'urgencyLevel': { en: 'Urgency:', ar: 'مستوى الاستعجال:' },
    'donorsNeeded': { en: 'donors needed', ar: 'متبرعين مطلوبين' },
    'chooseDay': { en: 'Choose Donation Day', ar: 'اختر يوم التبرع' },
    'chooseTime': { en: 'Choose Time Slot (Hours)', ar: 'اختر وقت الموعد (الساعة)' },
    'confirmBooking': { en: 'Confirm Booking', ar: 'تأكيد حجز الموعد' },
    'bookingAvailable': { en: 'Booking Available', ar: 'الحجز متاح حالياً' },

    // Donor Dashboard
    'statsSummary': { en: 'Personal Screening Summary', ar: 'ملخص الفحص الطبي والحيوي' },
    'screeningStatusText': { en: 'Your current medical screening status is:', ar: 'حالة أهليتك الطبية الحالية هي:' },
    'completedDonationsText': { en: 'Completed Donations', ar: 'التبرعات المكتملة' },
    'completedDonationsDesc': { en: 'Total verified blood donation check-ins.', ar: 'إجمالي عدد التبرعات الموثقة في السجل.' },
    'upcomingBookings': { en: 'Upcoming Appointments', ar: 'المواعيد المحجوزة القادمة' },
    'noActiveBookings': { en: 'No active bookings. You can register for blood drives from the feed page.', ar: 'لا توجد مواعيد نشطة محجوزة. يمكنك التسجيل في الحملات من صفحة التبرعات.' },
    'goBook': { en: 'Book a donation slot →', ar: 'احجز موعد تبرع بالدم الآن ←' },
    'cancel': { en: 'Cancel', ar: 'إلغاء الموعد' },
    'ticketInfo': { en: 'Present this QR code to the reception desk at the donation center for immediate check-in.', ar: 'يرجى إبراز رمز الاستجابة السريعة (QR) هذا لموظف الاستقبال بالمركز لتسجيل حضورك الفوري.' },

    // Admin Dashboard
    'overviewTab': { en: 'Overview', ar: 'نظرة عامة' },
    'checkinTab': { en: 'QR Check-In', ar: 'تسجيل الحضور' },
    'attendanceTab': { en: 'Attendance Log', ar: 'سجل الحضور والغياب' },
    'chartsTab': { en: 'Analytics & Charts', ar: 'التحليلات والمخططات البيانية' },
    'registeredDonors': { en: 'Registered Donors', ar: 'المتبرعون المسجلون' },
    'totalBookings': { en: 'Total Bookings', ar: 'إجمالي المواعيد المحجوزة' },
    'confirmedDonations': { en: 'Confirmed Donations', ar: 'التبرعات المؤكدة' },
    'checkedInAttendees': { en: 'Checked In Attendees', ar: 'المتبرعون الحاضرون' },
    'activeDrives': { en: 'Upcoming Blood Drives (Events)', ar: 'حملات التبرع بالدم النشطة' },
    'emergencyCalls': { en: 'Emergency Requests', ar: 'طلبات الطوارئ الفعالة' },
    'scanTitle': { en: 'QR Code Verification Check-In', ar: 'تسجيل الحضور عبر مسح رمز QR' },
    'scanPlaceholder': { en: 'Scan or Enter QR Code Token...', ar: 'امسح الرمز أو أدخل رمز المعرف هنا...' },
    'verifyCheckin': { en: 'Verify & Check-In', ar: 'التحقق وتسجيل الدخول' },
    'liveAttendance': { en: 'Live Attendance Log', ar: 'سجل الحضور اللحظي للمركز' },
    'nationalIdCol': { en: 'National ID', ar: 'الرقم الوطني' },
    'donorCol': { en: 'Donor Details', ar: 'بيانات المتبرع' },
    'bloodCol': { en: 'Blood', ar: 'الفصيلة' },
    'eventCol': { en: 'Event / Drive Venue', ar: 'موقع الحملة / المركز' },
    'checkinTimeCol': { en: 'Checked-In Time', ar: 'وقت تسجيل الحضور' },
    'statusCol': { en: 'Status', ar: 'الحالة' },
    'actionsCol': { en: 'Actions', ar: 'الإجراءات' },

    // Admin Users Management
    'donorManagement': { en: 'Manage Registered Donors', ar: 'إدارة سجلات المتبرعين' },
    'donorDirectory': { en: 'Donor Directory', ar: 'دليل المتبرعين الوطني' },
    'name': { en: 'Name', ar: 'الاسم الكامل' },
    'contact': { en: 'Contact Info', ar: 'بيانات الاتصال' },
    'screeningFormCol': { en: 'Screening Form', ar: 'نموذج الفحص الطبي' },
    'eligibility': { en: 'Eligibility', ar: 'الأهلية' },
    'reviewEdit': { en: 'Review & Edit Profile', ar: 'مراجعة وتعديل الملف الشخصي' },
    'saveChanges': { en: 'Save Changes', ar: 'حفظ التعديلات' },
    'needsVitalsAlert': { en: '⚠️ Needs Vitals check on arrival', ar: '⚠️ بحاجة لفحص العلامات الحيوية عند الوصول' },

    // Admin Posts
    'manageFeeds': { en: 'Manage Donation Feeds', ar: 'إدارة منشورات الحملات والطوارئ' },
    'liveCatalog': { en: 'Live Feed Catalog', ar: 'كتالوج المنشورات الفعال' },
    'createPost': { en: 'Create Post', ar: 'إنشاء منشور جديد' },
    'modifyPost': { en: 'Modify Post Details', ar: 'تعديل بيانات المنشور' },
    'publishRelease': { en: 'Publish Release', ar: 'نشر المنشور' },
    'title': { en: 'Title', ar: 'عنوان المنشور' },
    'description': { en: 'Description', ar: 'الوصف والتفاصيل' },
    'venue': { en: 'Location / Venue', ar: 'موقع الحملة / المستشفى' },
    'startDate': { en: 'Start Date & Time', ar: 'تاريخ ووقت البدء' },
    'endDate': { en: 'End Date & Time', ar: 'تاريخ ووقت الانتهاء' },
    'postType': { en: 'Post Type', ar: 'نوع المنشور' },
    'postStatus': { en: 'Post Status', ar: 'حالة المنشور' },
    'targetBlood': { en: 'Target Blood Group', ar: 'الفصيلة المستهدفة' },
    'urgency': { en: 'Urgency Level', ar: 'درجة الاستعجال' },
    'donorsNeededCount': { en: 'Donors Needed Count', ar: 'عدد المتبرعين المطلوبين' },
    'contactPhone': { en: 'Contact Phone', ar: 'رقم الهاتف للتواصل' },
    'expirationDate': { en: 'Expiration Date & Time', ar: 'تاريخ ووقت انتهاء الإعلان' },

    // Admin Appointments
    'manageBookingsTitle': { en: 'Donor Appointments Queue', ar: 'طابور مواعيد المتبرعين المحجوزة' },
    'donorName': { en: 'Donor Name', ar: 'اسم المتبرع' },
    'bookingTime': { en: 'Booking Date & Time', ar: 'تاريخ ووقت الموعد' },
    'complete': { en: 'Complete', ar: 'مكتمل' },
    'archive': { en: 'Archive', ar: 'أرشفة' },
    'delete': { en: 'Delete', ar: 'حذف' },
    'resendInfo': { en: 'Resend Details', ar: 'إعادة إرسال البيانات' },

    // Admin Reports
    'reportsTitle': { en: 'System Reports & Export Audits', ar: 'تقارير النظام والتدقيق الإحصائي' },
    'category': { en: 'Select Report Category', ar: 'اختر تصنيف التقرير' },
    'format': { en: 'Select Export Format', ar: 'اختر صيغة الملف المصدر' },
    'downloadReport': { en: 'Download Verified Report', ar: 'تحميل التقرير المعتمد' },

    // Mobile Navigation Short Labels
    'navOverview': { en: 'Overview', ar: 'الرئيسية' },
    'navFeeds': { en: 'Feeds', ar: 'الحملات' },
    'navDonors': { en: 'Donors', ar: 'المتبرعين' },
    'navBookings': { en: 'Bookings', ar: 'المواعيد' },
    'navReports': { en: 'Reports', ar: 'التقارير' }
  };

  constructor() {
    const saved = localStorage.getItem('bdms_lang') as 'en' | 'ar';
    if (saved === 'en' || saved === 'ar') {
      this.currentLang.set(saved);
    }
    this.updateDirection();
    
    // Auto-update HTML direction when language changes
    effect(() => {
      const lang = this.currentLang();
      localStorage.setItem('bdms_lang', lang);
      this.updateDirection();
    });
  }

  toggleLanguage() {
    this.currentLang.update(l => l === 'en' ? 'ar' : 'en');
  }

  t(key: string): string {
    const entry = this.dict[key];
    if (!entry) return key;
    return entry[this.currentLang()];
  }

  private updateDirection() {
    const lang = this.currentLang();
    document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
    document.documentElement.lang = lang;
  }
}
