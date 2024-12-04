import React, { useEffect, useState } from 'react';
import AuthService from '../services/AuthService';

const Home: React.FC = () => {
  const [loggedIn, setLoggedIn] = useState<boolean>(false);
  const [apiUrl, setApiUrl] = useState<string>('');

  useEffect(() => {
    setLoggedIn(AuthService.isLoggedIn());
    setApiUrl(AuthService.getApiUrl());
  }, []);

  const handleTestSecureEndpoint = async () => {
    try {
      const response = await fetch(`${apiUrl}Item/GetColorList`, {
        method: 'GET',
        credentials: 'include',
      });
      if (response.ok) {
        alert('Secure endpoint accessed successfully!');
      } else {
        alert('Failed to access secure endpoint.');
      }
    } catch (error) {
      console.error('Error accessing secure endpoint:', error);
      alert('Error accessing secure endpoint.');
    }
  };

  const handleLogout = async () => {
    await AuthService.logout();
    setLoggedIn(false);
  };

  return (
    <div className="container">
      <div className="card mt-3">
        <div className="card-header">
          <h3>Welcome to SimpleAuth for .NET</h3>
          <p>Making Auth Suck Less in .NET 9 WebApi and React</p>
        </div>
        <div className="card-body">
          {!loggedIn && (
            <div className="alert alert-warning">
              You are NOT logged in. <a href="/login">Log in</a>
            </div>
          )}
          {loggedIn && (
            <div className="alert alert-success">
              You are logged in. <button onClick={handleLogout}>Log out</button>
            </div>
          )}
        </div>
      </div>

      <div className="card mt-3">
        <div className="card-header">
          <h4>Test Area</h4>
          <p>Use the button below to test the secured endpoint.</p>
        </div>
        <div className="card-body">
          <button className="btn btn-primary" onClick={handleTestSecureEndpoint}>
            Test Secure Resource
          </button>
        </div>
      </div>
    </div>
  );
};

export default Home;
