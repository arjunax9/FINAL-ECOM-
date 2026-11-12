import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {
  form!: FormGroup;
  error = '';
  loading = false;

  constructor(private fb: FormBuilder, private auth: AuthService, private router: Router) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]]
    });
  }

  fillCustomer(): void {
    this.form.patchValue({
      email: 'demo@local',
      password: 'Password123'
    });
  }

  fillAdmin(): void {
    this.form.patchValue({
      email: 'admin@local',
      password: 'Admin123!'
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    this.error = '';
    const v = this.form.value;

    this.auth.login(v.email!, v.password!).subscribe({
      next: r => {
        this.loading = false;
        const token = (r as any)?.token;
        if (token) {
          this.auth.setToken(token);
          this.router.navigate(['/']);
        } else {
          this.error = 'Authentication succeeded but no token was provided.';
        }
      },
      error: (e) => {
        this.loading = false;
        if (e?.status === 0) {
          this.error = 'Cannot reach API Gateway. Please verify the backend is running.';
        } else if (e?.status === 502 || e?.status === 503) {
          this.error = `Service temporarily unavailable (${e.status} ${e.statusText || 'Bad Gateway'}). Please retry in a moment.`;
        } else {
          this.error = e?.error?.error || e?.error?.message || 'Invalid email or password. Please try again.';
        }
      }
    });
  }
}
