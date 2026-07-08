import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../services/api.service';
import { LanguageService } from '../services/language.service';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './admin-users.html'
})
export class AdminUsers implements OnInit {
  private api = inject(ApiService);
  protected readonly lang = inject(LanguageService);

  users = signal<any[]>([]);
  loading = signal(false);

  // Filters
  searchQuery = '';
  eligibilityFilter = '';
  bloodFilter = '';

  // Modal State
  showModal = signal(false);
  targetUser = signal<any | null>(null);

  // Edit fields
  editFullName = '';
  editNationalId = '';
  editMobileNumber = '';
  editEmail = '';
  editDateOfBirth = '';
  editGender = '';
  editNationality = '';
  editEligibility = 'Eligible';
  editBloodGroup = 'A';
  editRhFactor = '+';
  editWeight = 70;
  editHemoglobin = 14;
  editHematocrit = 42;
  editAdminNotes = '';

  ngOnInit() {
    this.loadUsers();
  }

  loadUsers() {
    this.loading.set(true);
    this.api.getAdminUsersList({
      search: this.searchQuery,
      eligibility: this.eligibilityFilter,
      bloodType: this.bloodFilter
    }).subscribe({
      next: (data) => {
        this.users.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  resetFilters() {
    this.searchQuery = '';
    this.eligibilityFilter = '';
    this.bloodFilter = '';
    this.loadUsers();
  }

  openReviewModal(user: any) {
    this.targetUser.set(user);
    this.editFullName = user.fullName || '';
    this.editNationalId = user.nationalId || '';
    this.editMobileNumber = user.mobileNumber || '';
    this.editEmail = user.email || '';
    this.editDateOfBirth = user.dateOfBirth || '';
    this.editGender = user.gender || '';
    this.editNationality = user.nationality || '';
    this.editEligibility = user.eligibilityStatus || 'Eligible';
    this.editBloodGroup = user.bloodGroup || 'A';
    this.editRhFactor = user.rhFactor || '+';
    this.editWeight = user.weight || 0;
    this.editHemoglobin = user.hemoglobin || 0;
    this.editHematocrit = user.hematocrit || 0;
    this.editAdminNotes = '';
    this.showModal.set(true);
  }

  closeReviewModal() {
    this.showModal.set(false);
  }

  submitReview() {
    const payload = {
      fullName: this.editFullName,
      nationalId: this.editNationalId,
      mobileNumber: this.editMobileNumber,
      email: this.editEmail,
      dateOfBirth: this.editDateOfBirth,
      gender: this.editGender,
      nationality: this.editNationality,
      eligibilityStatus: this.editEligibility,
      bloodGroup: this.editBloodGroup,
      rhFactor: this.editRhFactor,
      weight: this.editWeight,
      hemoglobin: this.editHemoglobin,
      hematocrit: this.editHematocrit,
      adminNotes: this.editAdminNotes || 'Reviewed manually by Admin.'
    };

    this.api.editUserProfile(this.targetUser().id, payload).subscribe({
      next: () => {
        this.closeReviewModal();
        this.loadUsers();
      },
      error: (err) => {
        alert(err.message || 'Failed to save changes.');
      }
    });
  }
}
