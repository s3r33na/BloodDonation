import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, Router } from '@angular/router';
import { ApiService } from '../services/api.service';
import { LanguageService } from '../services/language.service';

@Component({
  selector: 'app-admin-posts',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './admin-posts.html'
})
export class AdminPosts implements OnInit {
  private api = inject(ApiService);
  protected readonly lang = inject(LanguageService);
  
  posts = signal<any[]>([]);
  successMsg = signal('');

  // Form Drawer State
  showForm = signal(false);
  isEdit = signal(false);
  editPostId = 0;

  // Form Fields
  formType = 'Event';
  formStatus = 'Active';
  formTitle = '';
  formDesc = '';
  formLocation = '';
  // Event
  formStartDateTime = '';
  formEndDateTime = '';
  // Emergency
  formBloodType = 'Any';
  formUrgency = 'Medium';
  formDonorsNeeded = 1;
  formContactInfo = '';
  formExpiryTime = '';

  ngOnInit() {
    this.loadPosts();
  }

  loadPosts() {
    // Admins need to see all posts. For a management page, fetching active + completed + archived is preferred.
    // Fetch feed returns active by default, let's fetch active posts
    this.api.getFeed().subscribe({
      next: (data) => this.posts.set(data)
    });
  }

  openCreateForm() {
    this.isEdit.set(false);
    this.formType = 'Event';
    this.formStatus = 'Active';
    this.formTitle = '';
    this.formDesc = '';
    this.formLocation = '';
    this.formStartDateTime = '';
    this.formEndDateTime = '';
    this.formBloodType = 'Any';
    this.formUrgency = 'Medium';
    this.formDonorsNeeded = 1;
    this.formContactInfo = '';
    this.formExpiryTime = '';
    this.showForm.set(true);
  }

  openEditForm(post: any) {
    this.isEdit.set(true);
    this.editPostId = post.id;
    this.formType = post.type;
    this.formStatus = post.status;
    this.formTitle = post.title;
    this.formDesc = post.description;
    this.formLocation = post.location;
    
    if (post.type === 'Event') {
      this.formStartDateTime = post.startDateTime ? new Date(post.startDateTime).toISOString().slice(0, 16) : '';
      this.formEndDateTime = post.endDateTime ? new Date(post.endDateTime).toISOString().slice(0, 16) : '';
    } else {
      this.formBloodType = post.bloodType;
      this.formUrgency = post.urgencyLevel;
      this.formDonorsNeeded = post.donorsNeeded;
      this.formContactInfo = post.contactInfo;
      this.formExpiryTime = post.expiryTime ? new Date(post.expiryTime).toISOString().slice(0, 16) : '';
    }
    
    this.showForm.set(true);
  }

  closeForm() {
    this.showForm.set(false);
  }

  savePost() {
    const payload: any = {
      type: this.formType,
      title: this.formTitle,
      description: this.formDesc,
      location: this.formLocation,
      status: this.formStatus
    };

    if (this.formType === 'Event') {
      payload.startDateTime = this.formStartDateTime;
      payload.endDateTime = this.formEndDateTime;
    } else {
      payload.bloodType = this.formBloodType;
      payload.urgencyLevel = this.formUrgency;
      payload.donorsNeeded = this.formDonorsNeeded;
      payload.contactInfo = this.formContactInfo;
      payload.expiryTime = this.formExpiryTime;
    }

    if (this.isEdit()) {
      this.api.updatePost(this.editPostId, payload).subscribe({
        next: () => {
          this.successMsg.set('Post updated successfully!');
          this.closeForm();
          this.loadPosts();
        }
      });
    } else {
      this.api.createPost(payload).subscribe({
        next: () => {
          this.successMsg.set('New post published successfully!');
          this.closeForm();
          this.loadPosts();
        }
      });
    }
  }

  markComplete(id: number) {
    this.api.completePost(id).subscribe({
      next: () => {
        this.successMsg.set('Post marked as completed.');
        this.loadPosts();
      }
    });
  }

  archivePost(id: number) {
    this.api.archivePost(id).subscribe({
      next: () => {
        this.successMsg.set('Post archived successfully.');
        this.loadPosts();
      }
    });
  }

  deletePost(id: number) {
    if (confirm('Are you sure you want to permanently delete this post? This will delete all connected bookings.')) {
      this.api.deletePost(id).subscribe({
        next: () => {
          this.successMsg.set('Post deleted successfully.');
          this.loadPosts();
        }
      });
    }
  }
}
