import React, { useState } from 'react';
import AuthService from '../services/AuthService';
import { useNavigate } from 'react-router-dom';

const Register: React.FC = () => {
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [errors, setErrors] = useState<string[]>([]);
  const [checking, setChecking] = useState(false);
  const [available, setAvailable] = useState(false);
  const [checked, setChecked] = useState(false);
  const [metComplexity, setMetComplexity] = useState(true);
  const navigate = useNavigate();

  const onEmailChange = async (val: string) => {
    setEmail(val);
    setChecking(true);
    try {
      const exists = await AuthService.userExists(val);
      setAvailable(!exists);
      setChecked(true);
    } finally {
      setChecking(false);
    }
  };

  const onPasswordChange = async (val: string) => {
    setPassword(val);
    try {
      const result = await AuthService.checkPasswordComplexity(val);
      setMetComplexity(result.success);
      setErrors(result.errors || []);
    } catch {
      setErrors(['Unable to validate password']);
    }
  };

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrors([]);
    try {
      await AuthService.register({
        firstName,
        lastName,
        emailAddress: email,
        username: email,
        password,
        confirmPassword,
      });
      alert('Registration successful. Please log in.');
      navigate('/login');
    } catch (err: any) {
      if (err.response?.data?.errors) setErrors(err.response.data.errors);
      else alert('Registration failed');
    }
  };

  return (
    <div className="row justify-content-center">
      <div className="col-md-6">
        <h2 className="text-center">Register</h2>
        {errors.length > 0 && (
          <div className="alert alert-warning">
            <ul>{errors.map((e, i) => (<li key={i}>{e}</li>))}</ul>
          </div>
        )}
        <form onSubmit={onSubmit}>
          <div className="mb-3">
            <label className="form-label">Email</label>
            <input type="email" className="form-control" value={email} onChange={e => onEmailChange(e.target.value)} required />
            {checked && !available && <div className="text-danger">Email already in use.</div>}
          </div>
          <div className="mb-3">
            <label className="form-label">First Name</label>
            <input type="text" className="form-control" value={firstName} onChange={e => setFirstName(e.target.value)} required />
          </div>
          <div className="mb-3">
            <label className="form-label">Last Name</label>
            <input type="text" className="form-control" value={lastName} onChange={e => setLastName(e.target.value)} required />
          </div>
          <div className="mb-3">
            <label className="form-label">Password</label>
            <input type="password" className="form-control" value={password} onChange={e => onPasswordChange(e.target.value)} required />
            {!metComplexity && <div className="text-danger">Password doesn't meet complexity.</div>}
          </div>
          <div className="mb-3">
            <label className="form-label">Confirm Password</label>
            <input type="password" className="form-control" value={confirmPassword} onChange={e => setConfirmPassword(e.target.value)} required />
            {confirmPassword !== password && <div className="text-danger">Passwords must match.</div>}
          </div>
          <button type="submit" className="btn btn-primary" disabled={!available || password !== confirmPassword || !metComplexity || checking}>Register</button>
        </form>
      </div>
    </div>
  );
};

export default Register;
