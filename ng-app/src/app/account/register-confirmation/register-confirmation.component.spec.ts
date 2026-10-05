import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { RouterModule } from '@angular/router';

import { RegisterConfirmationComponent } from './register-confirmation.component';

describe('RegisterConfirmationComponent', () => {
  let component: RegisterConfirmationComponent;
  let fixture: ComponentFixture<RegisterConfirmationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MatCardModule, MatButtonModule, RouterModule.forRoot([])],
      declarations: [RegisterConfirmationComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RegisterConfirmationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
