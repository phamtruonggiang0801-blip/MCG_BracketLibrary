using System;
using System.Collections.Generic;
using Inventor;
using BracketLibraryInventorPlugin.Models;

namespace BracketLibraryInventorPlugin.Interop
{
    /// <summary>
    /// Bind 2 chiều giữa <see cref="BracketParameters"/> và Inventor User Parameters của .ipt.
    ///  - <see cref="Write"/>: tạo/cập nhật parameter (mm) → user thấy trong hộp thoại Parameters.
    ///  - <see cref="Read"/> : đọc lại giá trị user đã chỉnh tay để "Rebuild".
    /// </summary>
    internal static class BracketParameterBinder
    {
        private const string LengthUnit = "mm";

        public static void Write(PartComponentDefinition partDef, BracketParameters p)
        {
            var up = partDef.Parameters.UserParameters;
            foreach (var kv in p.ToParameterMap())
            {
                bool unitless = kv.Key == "Lip_Fold_Dir";
                string expr = unitless
                    ? kv.Value.ToString("0")
                    : $"{kv.Value.ToString("0.###")} {LengthUnit}";
                SetOrAdd(up, kv.Key, expr, unitless);
            }
        }

        public static BracketParameters Read(PartComponentDefinition partDef)
        {
            var map = new Dictionary<string, double>();
            foreach (Parameter prm in partDef.Parameters.UserParameters)
            {
                // Length parameters: Value = cm nội bộ → đổi ra mm. Lip_Fold_Dir: unitless.
                double val = prm.Name == "Lip_Fold_Dir" ? prm.Value : InventorUnits.Mm(prm.Value);
                map[prm.Name] = val;
            }
            return BracketParameters.FromParameterMap(map);
        }

        private static void SetOrAdd(UserParameters up, string name, string expr, bool unitless)
        {
            try
            {
                Parameter existing = up[name];
                existing.Expression = expr;
            }
            catch
            {
                if (unitless)
                    up.AddByValue(name, double.Parse(expr), UnitsTypeEnum.kUnitlessUnits);
                else
                    up.AddByExpression(name, expr, UnitsTypeEnum.kMillimeterLengthUnits);
            }
        }
    }
}
