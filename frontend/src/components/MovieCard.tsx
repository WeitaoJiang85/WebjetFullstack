import React, { useState } from "react";
import { FaHeart, FaRegHeart } from "react-icons/fa";
import { Movie } from "../types";
import placeholderPoster from "../assets/vertical.jpg";

interface MovieCardProps {
  movie: Movie;
  onClick: () => void;
  isSelected: boolean;
  isFavorite: boolean;
  toggleFavorite: () => void;
}

const MovieCard: React.FC<MovieCardProps> = ({
  movie,
  onClick,
  isSelected,
  isFavorite,
  toggleFavorite,
}) => {
  const [posterError, setPosterError] = useState(false);

  return (
    <div
      onClick={onClick}
      className={`relative flex items-center p-2 rounded-lg cursor-pointer transition transform ${
        isSelected
          ? "bg-yellow-500 text-black scale-105"
          : "bg-gray-800 text-white text-sm hover:scale-105"
      }`}
    >
      <img
        src={posterError ? placeholderPoster : movie.poster}
        alt={movie.title}
        className="w-16 h-24 object-cover rounded-lg"
        onError={() => setPosterError(true)}
      />
      <div className="ml-4 flex-grow">
        <h3 className="text-lg font-bold">{movie.title}</h3>
        <p className="text-white font-bold">Year: {movie.year}</p>
      </div>
      <button
        onClick={(e) => {
          e.stopPropagation();
          toggleFavorite();
        }}
      >
        {isFavorite ? (
          <FaHeart className="text-red-500" />
        ) : (
          <FaRegHeart className="text-gray-500" />
        )}
      </button>
    </div>
  );
};

export default MovieCard;
