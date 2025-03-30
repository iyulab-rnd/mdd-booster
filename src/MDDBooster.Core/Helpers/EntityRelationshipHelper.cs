namespace MDDBooster.Helpers;

public static class EntityRelationshipHelper
{
    // 관계 패턴 매핑 - 필드 이름과 생성할 컬렉션 속성 이름 간의 관계 정의
    private static readonly Dictionary<string, string> RelationshipPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        // 부모-자식 관계 패턴
        { "Parent", "Children" },
        { "Parent_id", "Children" },
        { "ParentId", "Children" },
        
        // 스레드 관계 패턴
        { "ThreadRoot", "ThreadReplies" },
        { "ThreadRoot_id", "ThreadReplies" },
        { "ThreadRootId", "ThreadReplies" },
        
        // 소유자 관계 패턴
        { "Owner", "OwnedItems" },
        { "Owner_id", "OwnedItems" },
        { "OwnerId", "OwnedItems" },
        
        // 작성자 관계 패턴
        { "Author", "AuthoredItems" },
        { "Author_id", "AuthoredItems" },
        { "AuthorId", "AuthoredItems" },
        
        // 기타 범용 관계 패턴
        { "Root", "Members" },
        { "Root_id", "Members" },
        { "RootId", "Members" }
    };

    /// <summary>
    /// 자기 참조 관계에서 적절한 컬렉션 속성 이름을 결정합니다.
    /// </summary>
    public static string DetermineCollectionPropertyName(string entityName, string foreignKeyFieldName)
    {
        // 기본 패턴 매핑 시도
        foreach (var pattern in RelationshipPatterns)
        {
            if (foreignKeyFieldName.Contains(pattern.Key, StringComparison.OrdinalIgnoreCase))
            {
                // entityName + pattern.Value 형식으로 반환
                // 예: Message + Children = MessageChildren
                return $"{entityName}{pattern.Value}";
            }
        }

        // 기본 패턴에 없는 경우 이름 분석 시도
        var baseName = Utils.GetNameWithoutKey(foreignKeyFieldName);

        // 자기 참조지만 특별한 패턴이 없는 경우 일반적인 컬렉션 이름 생성
        return $"{baseName}{entityName.ToPlural()}";
    }

    /// <summary>
    /// 외래 키 관계에서 역방향 참조 프로퍼티 이름을 결정합니다.
    /// </summary>
        public static string DetermineNavigationPropertyName(string relatedEntityName, string foreignKeyFieldName, string currentEntityName)
    {
        // 외래 키 필드에서 기본 이름 추출
        var baseName = Utils.GetNameWithoutKey(foreignKeyFieldName);

        // 기본 이름이 관련 엔티티와 같으면 충돌 방지
        if (baseName == relatedEntityName)
        {
            return $"{baseName}Item"; // 예: Message → MessageItem
        }

        // 기본 이름이 현재 엔티티와 같으면 충돌 방지
        if (baseName == currentEntityName)
        {
            return $"Related{baseName}"; // 예: User → RelatedUser
        }

        return baseName;
    }

    /// <summary>
    /// 자기 참조 관계인지 확인합니다.
    /// </summary>
    public static bool IsSelfReferencing(string entityName, string foreignKeyTarget)
    {
        return entityName.Equals(foreignKeyTarget, StringComparison.OrdinalIgnoreCase);
    }
}