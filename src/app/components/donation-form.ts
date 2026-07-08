import { Component, ElementRef, OnInit, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService, User } from '../services/api.service';
import { LanguageService } from '../services/language.service';
import { gsap } from 'gsap';

@Component({
  selector: 'app-donation-form',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './donation-form.html'
})
export class DonationForm implements OnInit {
  private api = inject(ApiService);
  private router = inject(Router);
  protected readonly lang = inject(LanguageService);

  user = this.api.currentUser;
  step = signal(1);
  loading = signal(false);
  errorMsg = signal('');

  // Form Fields
  address = '';
  bloodGroup = 'A';
  rhFactor = '+';
  weight: number | null = null;
  hemoglobin: number | null = null;
  hematocrit: number | null = null;
  dontKnowVitals = false;
  medConditions = '';
  recentIllness = '';

  // Yes/No Checklist
  qTattoo = false;
  qMeds = false;
  qSurgery = false;
  qChronic = false;
  qPreg = false;

  consented = false;

  @ViewChild('formCard', { static: true }) formCard!: ElementRef;

  ngOnInit() {
    // If not logged in, route to login
    if (!this.user()) {
      this.router.navigate(['/login']);
      return;
    }

    // prefill address if possible
    this.api.getDonationHistory().subscribe(history => {
      if (history && history.length > 0) {
        const lastForm = history[0];
        this.address = lastForm.address;
        this.bloodGroup = lastForm.bloodGroup;
        this.rhFactor = lastForm.rhFactor;
      }
    });

    gsap.fromTo(this.formCard.nativeElement,
      { opacity: 0, y: 15 },
      { opacity: 1, y: 0, duration: 0.5, ease: 'power2.out' }
    );
  }

  onNext() {
    this.errorMsg.set('');

    if (this.step() === 1) {
      if (!this.address.trim()) {
        this.errorMsg.set('Home Address is required.');
        return;
      }
    }

    if (this.step() === 3) {
      if (this.weight === null || this.weight < 30) {
        this.errorMsg.set('Please enter a valid weight.');
        return;
      }
      if (!this.dontKnowVitals) {
        if (this.hemoglobin === null || this.hemoglobin <= 0) {
          this.errorMsg.set('Please enter a valid hemoglobin level.');
          return;
        }
        if (this.hematocrit === null || this.hematocrit <= 0) {
          this.errorMsg.set('Please enter a valid hematocrit percentage.');
          return;
        }
      }
    }

    // Transition step
    this.animateStepChange(() => {
      this.step.update(s => s + 1);
    });
  }

  onPrev() {
    this.errorMsg.set('');
    this.animateStepChange(() => {
      this.step.update(s => s - 1);
    });
  }

  animateStepChange(callback: () => void) {
    gsap.to(this.formCard.nativeElement, {
      opacity: 0,
      x: -15,
      duration: 0.2,
      onComplete: () => {
        callback();
        gsap.fromTo(this.formCard.nativeElement,
          { opacity: 0, x: 15 },
          { opacity: 1, x: 0, duration: 0.3, ease: 'power2.out' }
        );
      }
    });
  }

  onSubmit() {
    this.errorMsg.set('');
    this.loading.set(true);

    const payload = {
      weight: this.weight,
      bloodGroup: this.bloodGroup,
      rhFactor: this.rhFactor,
      hemoglobin: this.dontKnowVitals ? 0 : this.hemoglobin,
      hematocrit: this.dontKnowVitals ? 0 : this.hematocrit,
      dontKnowVitals: this.dontKnowVitals,
      address: this.address,
      medicalConditions: this.medConditions || 'None',
      medications: this.qMeds ? 'Yes (Check antibiotics details)' : 'None',
      recentIllness: this.recentIllness || 'None',
      surgeryHistory: this.qSurgery ? 'Recent surgery indicated' : 'None',
      tattooOrPiercing: this.qTattoo,
      antibioticsOrMedications: this.qMeds,
      recentSurgery: this.qSurgery,
      chronicDisease: this.qChronic,
      pregnantOrBreastfeeding: this.qPreg
    };

    this.api.submitScreeningForm(payload).subscribe({
      next: (res) => {
        this.loading.set(false);
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMsg.set(err.message || 'Submission failed. Please check your data.');
      }
    });
  }
}
