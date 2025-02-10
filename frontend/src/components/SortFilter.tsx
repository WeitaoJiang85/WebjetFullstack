import React, { useState } from "react";
import { MenuItem, Select, FormControl } from "@mui/material";

interface SortFilterProps {
  onSortChange: (sortType: string, sortOrder: "asc" | "desc") => void;
}

const SortFilter: React.FC<SortFilterProps> = ({ onSortChange }) => {
  const [sortOption, setSortOption] = useState("rating-desc");

  const handleSortChange = (event: any) => {
    const selectedOption = event.target.value;
    setSortOption(selectedOption);

    const [type, order] = selectedOption.split("-");
    onSortChange(type, order as "asc" | "desc");
  };

  return (
    <FormControl
      fullWidth
      sx={{
        "& .MuiOutlinedInput-root": {
          color: "#FFD700",
          backgroundColor: "#222",
          "& fieldset": { borderColor: "#FFD700" },
          "&:hover fieldset": { borderColor: "#FFC107" },
          "&.Mui-focused fieldset": { borderColor: "#FFD700" },
        },
        "& .MuiSelect-select": {
          padding: "10px",
        },
      }}
    >
      <Select
        value={sortOption}
        onChange={handleSortChange}
        displayEmpty
        sx={{
          color: "#FFD700",
          "&:focus": { backgroundColor: "#111" },
        }}
        MenuProps={{
          PaperProps: {
            style: {
              backgroundColor: "#222",
              color: "#FFD700",
            },
          },
        }}
      >
        <MenuItem value="year-desc">Year (New → Old)</MenuItem>
        <MenuItem value="year-asc">Year (Old → New)</MenuItem>
        <MenuItem value="rating-desc">Rating (High → Low)</MenuItem>
        <MenuItem value="rating-asc">Rating (Low → High)</MenuItem>
        <MenuItem value="votes-desc">Votes (High → Low)</MenuItem>
        <MenuItem value="votes-asc">Votes (Low → High)</MenuItem>
        <MenuItem value="bestPrice-asc">Best Price (Low → High)</MenuItem>
        <MenuItem value="bestPrice-desc">Best Price (High → Low)</MenuItem>
      </Select>
    </FormControl>
  );
};

export default SortFilter;
