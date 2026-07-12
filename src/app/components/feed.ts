import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../services/api.service';
import { LanguageService } from '../services/language.service';

@Component({
  selector: 'app-feed',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './feed.html'
})
export class Feed implements OnInit {
  private api = inject(ApiService);
  private router = inject(Router);
  protected readonly lang = inject(LanguageService);

  user = this.api.currentUser;
  posts = signal<any[]>([]);
  loading = signal(false);
  errorMsg = signal('');
  successMsg = signal('');

  // Filters
  searchQuery = '';
  typeFilter = '';
  bloodFilter = '';

  // Booking Modal State
  selectedPost = signal<any | null>(null);
  bookingDateTime = '';
  bookingLoading = signal(false);
  bookingError = signal('');
  minDateTime = '';
  bookingStep = signal<number>(1);

  // Screening Questions
  feelGoodHealth = true;
  beenRejected = false;
  hadJaundiceOrHepatitis = false;
  hadMalaria = false;
  hadBrucellosisOrTyphoid = false;
  sideEffectsPrevious = false;
  traveledLast6Months = false;
  chronicDiseases = {
    blood: false,
    lungs: false,
    heart: false,
    kidneys: false,
    diabetes: false,
    bloodPressure: false
  };
  takingMedicationsOrInjections = false;
  faintingSpellsSeizures = false;
  severeAllergies = false;
  receivedVaccine14Days = false;
  tattooOrCupping12Months = false;
  surgeryOrTransfusion12Months = false;
  dentistLastWeek = false;
  pregnantOrRecentPregnancy = false;
  breastfeeding = false;
  threeOrMorePregnancies = false;

  consentBloodDraw = false;
  consentDeclaration = false;
  recommendDonatedBlood = true;
  receivedEducationalInfo = true;

  // Generated slots
  availableDays = signal<{ value: string, label: string }[]>([]);
  selectedDay = signal<string>('');
  availableHours = signal<string[]>([]);
  selectedHour = signal<string>('');

  ngOnInit() {
    this.loadFeed();
    const now = new Date();
    this.minDateTime = now.toISOString().slice(0, 16);
  }

  loadFeed() {
    this.loading.set(true);
    this.api.getFeed({
      type: this.typeFilter,
      bloodType: this.bloodFilter,
      search: this.searchQuery
    }).subscribe({
      next: (data) => {
        this.posts.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMsg.set('Failed to load feed posts. Please try again.');
      }
    });
  }

  resetFilters() {
    this.searchQuery = '';
    this.typeFilter = '';
    this.bloodFilter = '';
    this.loadFeed();
  }

  generateSlots(post: any) {
    const days: { value: string, label: string }[] = [];
    const startVal = post.startDateTime || post.eventDate;
    const endVal = post.endDateTime || post.eventDate;
    if (!startVal) return;

    const start = new Date(startVal);
    const end = new Date(endVal || startVal);
    
    // Normalize to dates
    const startDay = new Date(start.getFullYear(), start.getMonth(), start.getDate());
    const endDay = new Date(end.getFullYear(), end.getMonth(), end.getDate());
    
    const loopDate = new Date(startDay);
    while (loopDate <= endDay) {
      const dateStr = `${loopDate.getFullYear()}-${(loopDate.getMonth() + 1).toString().padStart(2, '0')}-${loopDate.getDate().toString().padStart(2, '0')}`;
      const label = loopDate.toLocaleDateString('en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });
      days.push({ value: dateStr, label });
      loopDate.setDate(loopDate.getDate() + 1);
    }
    
    this.availableDays.set(days);
    if (days.length > 0) {
      this.selectedDay.set(days[0].value);
      this.onDayChange();
    } else {
      this.selectedDay.set('');
      this.availableHours.set([]);
      this.selectedHour.set('');
    }
  }

  onDayChange() {
    const chosenDayStr = this.selectedDay();
    if (!chosenDayStr || !this.selectedPost()) return;

    const post = this.selectedPost();
    const startVal = post.startDateTime || post.eventDate;
    const endVal = post.endDateTime || post.eventDate;
    if (!startVal) return;

    const start = new Date(startVal);
    const end = new Date(endVal || startVal);

    const startHour = 8;
    const endHour = 17;
    const hours: string[] = [];

    const startDayStr = `${start.getFullYear()}-${(start.getMonth() + 1).toString().padStart(2, '0')}-${start.getDate().toString().padStart(2, '0')}`;
    const endDayStr = `${end.getFullYear()}-${(end.getMonth() + 1).toString().padStart(2, '0')}-${end.getDate().toString().padStart(2, '0')}`;

    let dayStartHour = startHour;
    let dayEndHour = endHour;

    if (chosenDayStr === startDayStr) {
      dayStartHour = Math.max(startHour, start.getHours());
    }
    if (chosenDayStr === endDayStr) {
      dayEndHour = Math.min(endHour, end.getHours());
    }

    for (let h = dayStartHour; h <= dayEndHour; h++) {
      const hStr = h.toString().padStart(2, '0') + ':00';
      hours.push(hStr);
    }

    this.availableHours.set(hours);
    if (hours.length > 0) {
      this.selectedHour.set(hours[0]);
    } else {
      this.selectedHour.set('');
    }
    
    this.updateBookingDateTime();
  }

  updateBookingDateTime() {
    if (this.selectedDay() && this.selectedHour()) {
      this.bookingDateTime = `${this.selectedDay()}T${this.selectedHour()}:00`;
    } else {
      this.bookingDateTime = '';
    }
  }

  onOpenBooking(post: any) {
    if (!this.user()) {
      this.router.navigate(['/login']);
      return;
    }
    this.selectedPost.set(post);
    this.bookingStep.set(1);
    this.bookingDateTime = '';
    this.bookingError.set('');
    
    // Reset all screening questions to default values
    this.feelGoodHealth = true;
    this.beenRejected = false;
    this.hadJaundiceOrHepatitis = false;
    this.hadMalaria = false;
    this.hadBrucellosisOrTyphoid = false;
    this.sideEffectsPrevious = false;
    this.traveledLast6Months = false;
    this.chronicDiseases = {
      blood: false,
      lungs: false,
      heart: false,
      kidneys: false,
      diabetes: false,
      bloodPressure: false
    };
    this.takingMedicationsOrInjections = false;
    this.faintingSpellsSeizures = false;
    this.severeAllergies = false;
    this.receivedVaccine14Days = false;
    this.tattooOrCupping12Months = false;
    this.surgeryOrTransfusion12Months = false;
    this.dentistLastWeek = false;
    this.pregnantOrRecentPregnancy = false;
    this.breastfeeding = false;
    this.threeOrMorePregnancies = false;
    this.consentBloodDraw = false;
    this.consentDeclaration = false;
    this.recommendDonatedBlood = true;
    this.receivedEducationalInfo = true;

    if (post.type === 'Event') {
      this.generateSlots(post);
    }
  }

  onCloseBooking() {
    this.selectedPost.set(null);
  }

  nextStep() {
    this.bookingError.set('');
    if (this.bookingStep() === 1) {
      this.updateBookingDateTime();
      if (!this.bookingDateTime) {
        this.bookingError.set('Please choose a date and time.');
        return;
      }
      this.bookingStep.set(2);
    } else if (this.bookingStep() === 2) {
      this.bookingStep.set(3);
    }
  }

  prevStep() {
    this.bookingError.set('');
    if (this.bookingStep() > 1) {
      this.bookingStep.update(s => s - 1);
    }
  }

  submitBooking() {
    this.updateBookingDateTime();
    if (!this.bookingDateTime) {
      this.bookingError.set('Please choose a date and time.');
      return;
    }

    if (!this.consentBloodDraw || !this.consentDeclaration) {
      this.bookingError.set('You must accept all consents to submit your booking.');
      return;
    }

    this.bookingLoading.set(true);
    this.bookingError.set('');
    this.successMsg.set('');

    // Compile chronic diseases list
    const chronicList: string[] = [];
    if (this.chronicDiseases.blood) chronicList.push('Blood');
    if (this.chronicDiseases.lungs) chronicList.push('Lungs');
    if (this.chronicDiseases.heart) chronicList.push('Heart');
    if (this.chronicDiseases.kidneys) chronicList.push('Kidneys');
    if (this.chronicDiseases.diabetes) chronicList.push('Diabetes');
    if (this.chronicDiseases.bloodPressure) chronicList.push('BloodPressure');

    // Compile female specific list
    const femaleList: string[] = [];
    if (this.user()?.gender === 'Female') {
      if (this.pregnantOrRecentPregnancy) femaleList.push('PregnantOrRecentPregnancy');
      if (this.breastfeeding) femaleList.push('Breastfeeding');
      if (this.threeOrMorePregnancies) femaleList.push('ThreeOrMorePregnancies');
    }

    const payload = {
      postId: this.selectedPost().id,
      appointmentDateTime: this.bookingDateTime,
      screening: {
        feelGoodHealth: this.feelGoodHealth,
        beenRejected: this.beenRejected,
        hadJaundiceOrHepatitis: this.hadJaundiceOrHepatitis,
        hadMalaria: this.hadMalaria,
        hadBrucellosisOrTyphoid: this.hadBrucellosisOrTyphoid,
        sideEffectsPrevious: this.sideEffectsPrevious,
        traveledLast6Months: this.traveledLast6Months,
        chronicDiseases: chronicList,
        takingMedicationsOrInjections: this.takingMedicationsOrInjections,
        faintingSpellsSeizures: this.faintingSpellsSeizures,
        severeAllergies: this.severeAllergies,
        receivedVaccine14Days: this.receivedVaccine14Days,
        tattooOrCupping12Months: this.tattooOrCupping12Months,
        surgeryOrTransfusion12Months: this.surgeryOrTransfusion12Months,
        dentistLastWeek: this.dentistLastWeek,
        femalePregnancyStatus: femaleList,
        consentBloodDraw: this.consentBloodDraw,
        consentDeclaration: this.consentDeclaration,
        recommendDonatedBlood: this.recommendDonatedBlood,
        receivedEducationalInfo: this.receivedEducationalInfo
      }
    };

    this.api.bookAppointment(payload).subscribe({
      next: () => {
        this.bookingLoading.set(false);
        this.successMsg.set('Appointment booked successfully! Confirmation notification generated.');
        this.onCloseBooking();
        this.loadFeed(); // Reload capacity slots
        window.scrollTo({ top: 0, behavior: 'smooth' });
        this.api.fetchNotifications().subscribe();
      },
      error: (err) => {
        this.bookingLoading.set(false);
        this.bookingError.set(err.message || 'Booking slot failed. Please verify conditions.');
      }
    });
  }
}
