using UnityEngine;

public static class Helper
{
    public static string FormatNumber(long number)
    {
        if (number < 0)
            return "-" + FormatNumber(-number);

        if (number < 1000)
            return number.ToString();

        if (number < 1000000)
            return $"{number / 1000f:0.#}K";

        if (number < 1000000000)
            return $"{number / 1000000f:0.#}M";

        if (number < 1000000000000L)
            return $"{number / 1000000000f:0.#}B";

        if (number < 1000000000000000L)
            return $"{number / 1000000000000f:0.#}T";

        if (number < 1000000000000000000L)
            return $"{number / 1000000000000000f:0.#}Qa";

        double dNumber = number;

        if (dNumber < 1e21) // Quintillion
            return $"{dNumber / 1e18:0.#}Qi";

        if (dNumber < 1e24) // Sextillion
            return $"{dNumber / 1e21:0.#}Sx";

        if (dNumber < 1e27) // Septillion
            return $"{dNumber / 1e24:0.#}Sp";

        if (dNumber < 1e30) // Octillion
            return $"{dNumber / 1e27:0.#}Oc";

        if (dNumber < 1e33) // Nonillion
            return $"{dNumber / 1e30:0.#}No";

        return "Кто ты, воин?";
    }
}