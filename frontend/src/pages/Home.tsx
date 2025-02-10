import React from "react";
import { MovieProvider } from "../context/MovieContext"; //
import MovieList from "../components/MovieList";
import MovieDetail from "../components/MovieDetail";

const Home: React.FC = () => {
  return (
    <MovieProvider>
      <div className="flex h-screen">
        <MovieList />
        <MovieDetail />
      </div>
    </MovieProvider>
  );
};

export default Home;
