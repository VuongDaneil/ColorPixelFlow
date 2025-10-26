using System.Collections.Generic;
using UnityEngine;

public static class PaintingSharedAttributes
{
    public static string KeyColorDefine = "KeyColor";
    public static string DefaultColorKey = "DefaultColor";
    public static string TransparentColorKey = "TransparentColor";

    public static void MoveRelative<T>(List<T> list, T itemToMove, T targetItem, bool higher)
    {
        if (list == null || itemToMove == null || targetItem == null)
            return;

        // Nếu 2 phần tử giống nhau thì không làm gì
        if (EqualityComparer<T>.Default.Equals(itemToMove, targetItem))
            return;

        // Lấy index hiện tại của item và target
        int currentIndex = list.IndexOf(itemToMove);
        int targetIndex = list.IndexOf(targetItem);

        // Nếu không tìm thấy một trong hai thì bỏ qua
        if (currentIndex == -1 || targetIndex == -1)
            return;

        // Bỏ item ra khỏi list
        list.RemoveAt(currentIndex);

        // Nếu item nằm TRƯỚC target ban đầu, thì index của target đã thay đổi sau khi Remove
        if (currentIndex < targetIndex)
            targetIndex--;

        // Tính index mới để chèn vào
        int newIndex = higher ? targetIndex : targetIndex + 1;

        // Giới hạn index trong phạm vi hợp lệ
        newIndex = Mathf.Clamp(newIndex, 0, list.Count);

        // Chèn lại item
        list.Insert(newIndex, itemToMove);
    }

    public static void InsertRelative<T>(List<T> list, T newItem, T targetItem, bool higher)
    {
        if (list == null || newItem == null || targetItem == null)
            return;

        int targetIndex = list.IndexOf(targetItem);
        if (targetIndex == -1)
        {
            // Nếu targetItem không có trong list, thêm vào cuối danh sách
            list.Add(newItem);
            return;
        }

        int insertIndex = higher ? targetIndex : targetIndex + 1;
        insertIndex = Mathf.Clamp(insertIndex, 0, list.Count);

        list.Insert(insertIndex, newItem);
    }
}
