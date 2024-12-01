import React from 'react';

const Home: React.FC = () => {
  return (
    <div className="row justify-content-center">
      <div className="col-md-8">
        <div className="card">
          <div className="card-body">
            <h5 className="card-title">Welcome to the Home Page</h5>
            <p className="card-text">This is a simple React app styled with Bootstrap 5.</p>
            <a href="/login" className="btn btn-primary">Go to Login</a>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Home;
