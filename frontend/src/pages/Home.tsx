import React, { useContext } from "react";
import { MovieContext } from "../context/MovieContext";
import MovieList from "../components/MovieList";
import MovieDetail from "../components/MovieDetail";

const Home: React.FC = () => {
  const context = useContext(MovieContext);
  if (!context) return null;
  const { selectedMovie } = context;

  return (
    <div className="flex h-screen">
      <MovieList />
      <MovieDetail movie={selectedMovie} />
    </div>
  );
};

export default Home;
