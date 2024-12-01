import React, { useState } from 'react';
import AuthService from '../services/AuthService';
import { useConfig } from '../contexts/ConfigContext';
import { Link } from 'react-router-dom';

const Login: React.FC = () => {
  const [username, setUsername] = useState<string>('');
  const [password, setPassword] = useState<string>('');
  const config = useConfig();

  const handleLogin = async () => {
    try {
      await AuthService.login({ username, password, deviceId: AuthService['deviceId'] });
      alert('Login successful!');
    } catch (error) {
      alert('Login failed!');
    }
  };

  return (
    <div className="row justify-content-center">
      <div className="col-md-6">
        <h2 className="text-center">Login</h2>
        <form>
          <div className="mb-3">
            <label htmlFor="username" className="form-label">Username</label>
            <input
              type="text"
              className="form-control"
              id="username"
              placeholder="Enter your username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
            />
          </div>
          <div className="mb-3">
            <label htmlFor="password" className="form-label">Password</label>
            <input
              type="password"
              className="form-control"
              id="password"
              placeholder="Enter your password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
          </div>
          <button
            type="button"
            className="btn btn-primary w-100"
            onClick={handleLogin}
          >
            Login
          </button>
        </form>
        <div className="d-flex justify-content-between mt-3">
          <a href="#">Forgot Password?</a>
          {config.allowRegistration && <Link to="/register">Register</Link>}
        </div>
      </div>
    </div>
  );
};

export default Login;
