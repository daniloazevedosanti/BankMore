using System.Linq;
namespace Shared;
public static class CpfValidator
{
    // Validates Brazilian CPF (11 digits) with checksum
    public static bool IsValid(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf)) return false;
        var digits = new string(cpf.Where(char.IsDigit).ToArray());
        if (digits.Length != 11) return false;
        // Reject repeated digits
        if (new string(digits[0], 11) == digits) return false;
        int[] nums = digits.Select(c => c - '0').ToArray();
        // first check digit
        int sum = 0;
        for (int i = 0; i < 9; i++) sum += nums[i] * (10 - i);
        int rem = sum % 11;
        int d1 = (rem < 2) ? 0 : 11 - rem;
        if (d1 != nums[9]) return false;
        // second check digit
        sum = 0;
        for (int i = 0; i < 10; i++) sum += nums[i] * (11 - i);
        rem = sum % 11;
        int d2 = (rem < 2) ? 0 : 11 - rem;
        return d2 == nums[10];
    }
}
