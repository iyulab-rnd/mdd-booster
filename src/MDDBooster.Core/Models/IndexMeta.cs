namespace MDDBooster.Models;

public class IndexMeta
{
    public List<string> Columns { get; set; } = new List<string>();
    public string? Name { get; set; }

    // 인덱스의 실제 이름을 생성하는 메서드 추가
    public string GetIndexName(string tableName)
    {
        // 사용자 정의 이름이 있으면 사용
        if (!string.IsNullOrEmpty(Name))
        {
            return Name;
        }

        // 없으면 테이블명과 컬럼명을 조합하여 이름 생성
        return $"IX_{tableName}_{string.Join("_", Columns)}";
    }
}